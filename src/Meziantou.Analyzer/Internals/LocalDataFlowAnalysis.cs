using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Internals;

internal static class LocalDataFlowAnalysis
{
    public static ITypeSymbol? GetActualType(this IOperation operation, CancellationToken cancellationToken)
    {
        operation = operation.UnwrapImplicitConversionOperations();

        var value = GetFlowValue(operation, cancellationToken);
        if (value is not null && value != operation)
            return GetActualType(value, cancellationToken);

        return operation.Type;
    }

    public static bool TryGetConstantValue(this IOperation operation, out object? value, CancellationToken cancellationToken)
    {
        operation = operation.UnwrapImplicitConversionOperations();
        if (operation.ConstantValue.HasValue)
        {
            value = operation.ConstantValue.Value;
            return true;
        }

        var flowValue = GetFlowValue(operation, cancellationToken);
        if (flowValue is not null && flowValue != operation)
            return TryGetConstantValue(flowValue, out value, cancellationToken);

        value = null;
        return false;
    }

    private static IOperation? GetFlowValue(IOperation operation, CancellationToken cancellationToken)
    {
        return operation switch
        {
            ILocalReferenceOperation localReference => GetLocalValue(localReference, cancellationToken),
            IFieldReferenceOperation fieldReference => GetFieldValue(fieldReference, cancellationToken),
            IPropertyReferenceOperation propertyReference => GetPropertyValue(propertyReference, cancellationToken),
            _ => null,
        };
    }

    private static IOperation? GetLocalValue(ILocalReferenceOperation operation, CancellationToken cancellationToken)
    {
        if (operation.SemanticModel is null)
            return null;

        var local = operation.Local;
        if (IsCaptured(operation.SemanticModel, local, operation.Syntax))
            return null;

        var value = GetLastLocalAssignment(operation, local, cancellationToken);
        if (value is not null)
            return value;

        if (GetLocalInitializer(operation.SemanticModel, local, cancellationToken) is not { } initializer)
            return null;

        return HasWriteBetween(operation.SemanticModel, local, initializer.Syntax, operation.Syntax, cancellationToken) ? null : initializer;
    }

    private static IOperation? GetLastLocalAssignment(IOperation operation, ILocalSymbol local, CancellationToken cancellationToken)
    {
        var semanticModel = operation.SemanticModel;
        if (semanticModel is null)
            return null;

        IOperation? result = null;
        foreach (var assignmentSyntax in operation.Syntax.SyntaxTree.GetRoot(cancellationToken).DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (assignmentSyntax.Span.End > operation.Syntax.SpanStart)
                continue;

            if (IsInNestedFunction(assignmentSyntax))
                continue;

            if (semanticModel.GetOperation(assignmentSyntax, cancellationToken) is not ISimpleAssignmentOperation assignment)
                continue;

            if (assignment.Target.UnwrapImplicitConversionOperations() is not ILocalReferenceOperation localReference || !localReference.Local.IsEqualTo(local))
                continue;

            if (result is null || assignment.Syntax.SpanStart > result.Syntax.SpanStart)
            {
                result = assignment.Value;
            }
        }

        if (result is null)
            return null;

        return HasWriteBetween(semanticModel, local, result.Syntax, operation.Syntax, cancellationToken) ? null : result;
    }

    private static IOperation? GetLocalInitializer(SemanticModel semanticModel, ILocalSymbol local, CancellationToken cancellationToken)
    {
        foreach (var syntaxReference in local.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(cancellationToken) is VariableDeclaratorSyntax { Initializer.Value: { } value })
            {
                if (!TryGetSemanticModel(semanticModel, value, out var initializerSemanticModel))
                    return null;

                return initializerSemanticModel.GetOperation(value, cancellationToken);
            }
        }

        return null;
    }

    private static IOperation? GetFieldValue(IFieldReferenceOperation operation, CancellationToken cancellationToken)
    {
        var semanticModel = operation.SemanticModel;
        if (semanticModel is null)
            return null;

        var field = operation.Field;
        if (!field.IsReadOnly || field.DeclaredAccessibility is not Accessibility.Private)
            return null;

        foreach (var syntaxReference in field.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(cancellationToken) is not VariableDeclaratorSyntax { Initializer.Value: { } value })
                continue;

            if (HasAssignmentOutsideInitializer(semanticModel, field, value, cancellationToken))
                return null;

            if (!TryGetSemanticModel(semanticModel, value, out var initializerSemanticModel))
                return null;

            return initializerSemanticModel.GetOperation(value, cancellationToken);
        }

        return null;
    }

    private static IOperation? GetPropertyValue(IPropertyReferenceOperation operation, CancellationToken cancellationToken)
    {
        var semanticModel = operation.SemanticModel;
        if (semanticModel is null)
            return null;

        var property = operation.Property;
        if (property.SetMethod is not null || property.DeclaredAccessibility is not Accessibility.Private)
            return null;

        foreach (var syntaxReference in property.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(cancellationToken) is not PropertyDeclarationSyntax { Initializer.Value: { } value })
                continue;

            if (HasAssignmentOutsideInitializer(semanticModel, property, value, cancellationToken))
                return null;

            if (!TryGetSemanticModel(semanticModel, value, out var initializerSemanticModel))
                return null;

            return initializerSemanticModel.GetOperation(value, cancellationToken);
        }

        return null;
    }

    private static bool HasAssignmentOutsideInitializer(SemanticModel semanticModel, ISymbol symbol, ExpressionSyntax initializer, CancellationToken cancellationToken)
    {
        foreach (var syntaxReference in symbol.ContainingType.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax typeDeclaration)
                continue;

            foreach (var assignment in typeDeclaration.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (assignment.SyntaxTree == initializer.SyntaxTree && initializer.Span.Contains(assignment.Span))
                    continue;

                if (!TryGetSemanticModel(semanticModel, assignment, out var assignmentSemanticModel))
                    return true;

                var targetSymbol = assignmentSemanticModel.GetSymbolInfo(assignment.Left, cancellationToken).Symbol;
                if (targetSymbol.IsEqualTo(symbol))
                    return true;
            }
        }

        return false;
    }

    private static bool IsCaptured(SemanticModel semanticModel, ISymbol local, SyntaxNode syntax)
    {
        var statement = syntax.FirstAncestorOrSelf<StatementSyntax>();
        if (statement is null)
            return false;

        var dataFlow = semanticModel.AnalyzeDataFlow(statement);
        return dataFlow?.Succeeded == true && ContainsSymbol(dataFlow.Captured, local);
    }

    private static bool HasWriteBetween(SemanticModel semanticModel, ISymbol symbol, SyntaxNode source, SyntaxNode destination, CancellationToken cancellationToken)
    {
        if (source.SyntaxTree != destination.SyntaxTree)
            return true;

        if (!TryGetStatementRange(source, destination, out var firstStatement, out var lastStatement))
            return true;

        if (firstStatement is null || lastStatement is null)
            return false;

        if (firstStatement is not StatementSyntax || lastStatement is not StatementSyntax)
            return HasWriteBetweenBySyntax(semanticModel, symbol, source, destination, cancellationToken);

        var dataFlow = semanticModel.AnalyzeDataFlow(firstStatement, lastStatement);
        if (dataFlow?.Succeeded != true)
            return true;

        return ContainsSymbol(dataFlow.WrittenInside, symbol) ||
            ContainsSymbol(dataFlow.UnsafeAddressTaken, symbol) ||
            ContainsSymbol(dataFlow.Captured, symbol);
    }

    private static bool HasWriteBetweenBySyntax(SemanticModel semanticModel, ISymbol symbol, SyntaxNode source, SyntaxNode destination, CancellationToken cancellationToken)
    {
        if (source.SyntaxTree != destination.SyntaxTree)
            return true;

        var sourceEnd = source.Span.End;
        var destinationStart = destination.SpanStart;
        foreach (var assignment in destination.SyntaxTree.GetRoot(cancellationToken).DescendantNodes())
        {
            if (assignment.SpanStart < sourceEnd || assignment.Span.End > destinationStart)
                continue;

            SyntaxNode? target = assignment switch
            {
                AssignmentExpressionSyntax assignmentExpression => assignmentExpression.Left,
                PrefixUnaryExpressionSyntax prefixUnaryExpression
                    when prefixUnaryExpression.IsKind(SyntaxKind.PreIncrementExpression) || prefixUnaryExpression.IsKind(SyntaxKind.PreDecrementExpression) => prefixUnaryExpression.Operand,
                PostfixUnaryExpressionSyntax postfixUnaryExpression
                    when postfixUnaryExpression.IsKind(SyntaxKind.PostIncrementExpression) || postfixUnaryExpression.IsKind(SyntaxKind.PostDecrementExpression) => postfixUnaryExpression.Operand,
                _ => null,
            };

            if (target is null)
                continue;

            var targetSymbol = semanticModel.GetSymbolInfo(target, cancellationToken).Symbol;
            if (targetSymbol.IsEqualTo(symbol))
                return true;
        }

        return false;
    }

    private static bool TryGetSemanticModel(SemanticModel semanticModel, SyntaxNode syntax, [NotNullWhen(true)] out SemanticModel? result)
    {
        if (semanticModel.SyntaxTree == syntax.SyntaxTree)
        {
            result = semanticModel;
            return true;
        }

        if (!semanticModel.Compilation.ContainsSyntaxTree(syntax.SyntaxTree))
        {
            result = null;
            return false;
        }

        result = semanticModel.Compilation.GetSemanticModel(syntax.SyntaxTree);
        return true;
    }

    private static bool TryGetStatementRange(SyntaxNode source, SyntaxNode destination, out SyntaxNode? firstStatement, out SyntaxNode? lastStatement)
    {
        firstStatement = null;
        lastStatement = null;

        var sourceStatement = source.FirstAncestorOrSelf<StatementSyntax>();
        var destinationStatement = destination.FirstAncestorOrSelf<StatementSyntax>();
        if (sourceStatement is null || destinationStatement is null)
            return false;

        if (sourceStatement == destinationStatement)
            return true;

        if (sourceStatement.Parent is not BlockSyntax sourceBlock || destinationStatement.Parent is not BlockSyntax destinationBlock || sourceBlock != destinationBlock)
        {
            return TryGetGlobalStatementRange(sourceStatement, destinationStatement, out firstStatement, out lastStatement);
        }

        var sourceIndex = sourceBlock.Statements.IndexOf(sourceStatement);
        var destinationIndex = sourceBlock.Statements.IndexOf(destinationStatement);
        if (sourceIndex < 0 || destinationIndex < 0 || sourceIndex > destinationIndex)
            return false;

        if (sourceIndex + 1 >= destinationIndex)
            return true;

        firstStatement = sourceBlock.Statements[sourceIndex + 1];
        lastStatement = sourceBlock.Statements[destinationIndex - 1];
        return true;
    }

    private static bool TryGetGlobalStatementRange(StatementSyntax sourceStatement, StatementSyntax destinationStatement, out SyntaxNode? firstStatement, out SyntaxNode? lastStatement)
    {
        firstStatement = null;
        lastStatement = null;

        if (sourceStatement.Parent is not GlobalStatementSyntax sourceGlobalStatement ||
            destinationStatement.Parent is not GlobalStatementSyntax destinationGlobalStatement ||
            sourceGlobalStatement.Parent is not CompilationUnitSyntax sourceCompilationUnit ||
            destinationGlobalStatement.Parent is not CompilationUnitSyntax destinationCompilationUnit ||
            sourceCompilationUnit != destinationCompilationUnit)
        {
            return false;
        }

        var sourceIndex = sourceCompilationUnit.Members.IndexOf(sourceGlobalStatement);
        var destinationIndex = sourceCompilationUnit.Members.IndexOf(destinationGlobalStatement);
        if (sourceIndex < 0 || destinationIndex < 0 || sourceIndex > destinationIndex)
            return false;

        if (sourceIndex + 1 >= destinationIndex)
            return true;

        firstStatement = sourceCompilationUnit.Members[sourceIndex + 1];
        lastStatement = sourceCompilationUnit.Members[destinationIndex - 1];
        return true;
    }

    private static bool IsInNestedFunction(SyntaxNode syntax)
    {
        return syntax.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>() is not null ||
            syntax.FirstAncestorOrSelf<LocalFunctionStatementSyntax>() is not null;
    }

    private static bool ContainsSymbol(IEnumerable<ISymbol> symbols, ISymbol symbol)
    {
        foreach (var candidate in symbols)
        {
            if (candidate.IsEqualTo(symbol))
                return true;
        }

        return false;
    }
}

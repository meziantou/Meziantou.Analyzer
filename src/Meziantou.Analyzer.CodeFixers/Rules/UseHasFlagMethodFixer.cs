using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseHasFlagMethodFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseHasFlagMethod);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (!TryGetHasFlagPattern(semanticModel, nodeToFix, context.CancellationToken, out var pattern))
            return;

        const string Title = "Use HasFlag";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => UseHasFlag(context.Document, pattern.OperationSpan, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Document> UseHasFlag(Document document, TextSpan operationSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var nodeToFix = root.FindNode(operationSpan, getInnermostNodeForTie: true);

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return document;

        if (!TryGetHasFlagPattern(semanticModel, nodeToFix, cancellationToken, out var pattern))
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var target = AddParenthesesIfNeeded(pattern.EnumValueExpression.WithoutTrivia());

        var replacementNode = InvocationExpression(
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, target, IdentifierName(nameof(Enum.HasFlag))),
            ArgumentList(SingletonSeparatedList(Argument(pattern.FlagExpression.WithoutTrivia()))));

        ExpressionSyntax updatedNode = replacementNode;
        if (pattern.Negate)
        {
            updatedNode = PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, updatedNode);
        }

        editor.ReplaceNode(pattern.OperationExpression, updatedNode.WithTriviaFrom(pattern.OperationExpression).WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static ExpressionSyntax AddParenthesesIfNeeded(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax => expression,
            GenericNameSyntax => expression,
            ThisExpressionSyntax => expression,
            BaseExpressionSyntax => expression,
            MemberAccessExpressionSyntax => expression,
            InvocationExpressionSyntax => expression,
            ElementAccessExpressionSyntax => expression,
            ParenthesizedExpressionSyntax => expression,
            _ => expression.Parentheses(),
        };
    }

    private static bool TryGetHasFlagPattern(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken, [NotNullWhen(true)] out HasFlagPattern? pattern)
    {
        foreach (var candidateNode in node.AncestorsAndSelf())
        {
            var operation = semanticModel.GetOperation(candidateNode, cancellationToken);
            if (operation is null)
                continue;

            if (TryGetHasFlagPattern(operation, out pattern))
                return true;
        }

        pattern = null;
        return false;
    }

    private static bool TryGetHasFlagPattern(IOperation operation, [NotNullWhen(true)] out HasFlagPattern? pattern)
    {
        if (operation is IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals } binaryOperation)
        {
            pattern = GetFromBinaryComparison(binaryOperation);
            return pattern is not null;
        }

        if (operation is IIsPatternOperation
            {
                Value: IBinaryOperation { OperatorKind: BinaryOperatorKind.And } andOperation,
                Pattern: IPatternOperation patternOperation,
                Syntax: ExpressionSyntax operationExpression,
            })
        {
            if (TryGetComparedOperand(patternOperation, out var comparedOperand, out var negate))
            {
                pattern = GetFromBitwiseAnd(andOperation, comparedOperand, operationExpression, negate);
                return pattern is not null;
            }
        }

        pattern = null;
        return false;
    }

    private static bool TryGetComparedOperand(IPatternOperation patternOperation, [NotNullWhen(true)] out IOperation? comparedOperand, out bool negate)
    {
        if (patternOperation is IConstantPatternOperation { Value: not null } constantPattern)
        {
            comparedOperand = constantPattern.Value;
            negate = false;
            return true;
        }

        if (patternOperation is INegatedPatternOperation { Pattern: IConstantPatternOperation { Value: not null } constantPattern2 })
        {
            comparedOperand = constantPattern2.Value;
            negate = true;
            return true;
        }

        comparedOperand = null;
        negate = false;
        return false;
    }

    private static HasFlagPattern? GetFromBinaryComparison(IBinaryOperation operation)
    {
        var leftOperand = operation.LeftOperand.UnwrapImplicitConversionOperations();
        var rightOperand = operation.RightOperand.UnwrapImplicitConversionOperations();
        var negate = operation.OperatorKind is BinaryOperatorKind.NotEquals;

        if (operation.Syntax is not ExpressionSyntax operationExpression)
            return null;

        if (leftOperand is IBinaryOperation { OperatorKind: BinaryOperatorKind.And } leftBitwiseAnd)
        {
            var pattern = GetFromBitwiseAnd(leftBitwiseAnd, rightOperand, operationExpression, negate);
            if (pattern is not null)
                return pattern;
        }

        if (rightOperand is IBinaryOperation { OperatorKind: BinaryOperatorKind.And } rightBitwiseAnd)
        {
            var pattern = GetFromBitwiseAnd(rightBitwiseAnd, leftOperand, operationExpression, negate);
            if (pattern is not null)
                return pattern;
        }

        return null;
    }

    private static HasFlagPattern? GetFromBitwiseAnd(IBinaryOperation bitwiseAndOperation, IOperation comparedOperand, ExpressionSyntax operationExpression, bool negate)
    {
        var leftOperand = bitwiseAndOperation.LeftOperand.UnwrapImplicitConversionOperations();
        var rightOperand = bitwiseAndOperation.RightOperand.UnwrapImplicitConversionOperations();
        comparedOperand = comparedOperand.UnwrapImplicitConversionOperations();

        if (TryGetEnumFlagReference(rightOperand, comparedOperand, out var flagOperation) &&
            IsValidPattern(leftOperand, flagOperation) &&
            leftOperand.Syntax is ExpressionSyntax enumValueExpression &&
            flagOperation.Syntax is ExpressionSyntax flagExpression)
        {
            return new(operationExpression, enumValueExpression, flagExpression, negate);
        }

        if (TryGetEnumFlagReference(leftOperand, comparedOperand, out flagOperation) &&
            IsValidPattern(rightOperand, flagOperation) &&
            rightOperand.Syntax is ExpressionSyntax enumValueExpression2 &&
            flagOperation.Syntax is ExpressionSyntax flagExpression2)
        {
            return new(operationExpression, enumValueExpression2, flagExpression2, negate);
        }

        return null;
    }

    private static bool TryGetEnumFlagReference(IOperation potentialFlag, IOperation comparedOperand, [NotNullWhen(true)] out IFieldReferenceOperation? flagOperation)
    {
        potentialFlag = potentialFlag.UnwrapImplicitConversionOperations();
        comparedOperand = comparedOperand.UnwrapImplicitConversionOperations();

        if (potentialFlag is IFieldReferenceOperation firstFieldReference &&
            comparedOperand is IFieldReferenceOperation secondFieldReference &&
            firstFieldReference.Field.HasConstantValue &&
            secondFieldReference.Field.HasConstantValue &&
            firstFieldReference.Field.IsEqualTo(secondFieldReference.Field) &&
            firstFieldReference.Field.ContainingType.IsEnumeration())
        {
            flagOperation = secondFieldReference;
            return true;
        }

        flagOperation = null;
        return false;
    }

    private static bool IsValidPattern(IOperation enumValueOperation, IOperation flagOperation)
    {
        if (enumValueOperation.Type is null || flagOperation.Type is null)
            return false;

        if (!enumValueOperation.Type.IsEnumeration())
            return false;

        if (!flagOperation.Type.IsEnumeration())
            return false;

        return enumValueOperation.Type.IsEqualTo(flagOperation.Type);
    }

    private sealed record HasFlagPattern(ExpressionSyntax OperationExpression, ExpressionSyntax EnumValueExpression, ExpressionSyntax FlagExpression, bool Negate)
    {
        public TextSpan OperationSpan => OperationExpression.Span;
    }
}

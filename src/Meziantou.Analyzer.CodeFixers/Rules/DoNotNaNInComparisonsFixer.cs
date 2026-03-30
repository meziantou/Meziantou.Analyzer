using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class DoNotNaNInComparisonsFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.DoNotNaNInComparisons);

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

        var halfTypeSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.Half");

        var binaryExpression = nodeToFix.FirstAncestorOrSelf<BinaryExpressionSyntax>();
        if (binaryExpression is null)
            return;

        var binaryOperation = semanticModel.GetOperation(binaryExpression, context.CancellationToken) as IBinaryOperation;
        if (binaryOperation is null)
            return;

        var leftIsNaN = TryGetNaNType(binaryOperation.LeftOperand, halfTypeSymbol, out _, out var leftSyntax);
        var rightIsNaN = TryGetNaNType(binaryOperation.RightOperand, halfTypeSymbol, out _, out var rightSyntax);
        if (leftIsNaN && leftSyntax is not null && nodeToFix.IsEquivalentTo(leftSyntax))
        {
            RegisterCodeFix(binaryOperation.LeftOperand);
        }

        if (rightIsNaN && rightSyntax is not null && nodeToFix.IsEquivalentTo(rightSyntax))
        {
            RegisterCodeFix(binaryOperation.RightOperand);
        }

        void RegisterCodeFix(IOperation nanOperand)
        {
            var title = "Use IsNaN";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => FixComparison(context.Document, binaryOperation, nanOperand, ct),
                    equivalenceKey: title),
                context.Diagnostics);
        }
    }

    private static async Task<Document> FixComparison(Document document, IBinaryOperation binaryOperation, IOperation nanOperand, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var halfTypeSymbol = editor.SemanticModel.Compilation.GetBestTypeByMetadataName("System.Half");
        if (!TryGetReplacementExpression(editor.Generator, binaryOperation, nanOperand, halfTypeSymbol, out var replacement))
            return document;

        editor.ReplaceNode(binaryOperation.Syntax, replacement.WithTriviaFrom(binaryOperation.Syntax));
        return editor.GetChangedDocument();
    }

    private static bool TryGetReplacementExpression(SyntaxGenerator generator, IBinaryOperation binaryOperation, IOperation nanOperand, INamedTypeSymbol? halfTypeSymbol, out ExpressionSyntax replacement)
    {
        if (binaryOperation.OperatorKind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals))
        {
            replacement = null!;
            return false;
        }

        var leftIsNaN = TryGetNaNType(binaryOperation.LeftOperand, halfTypeSymbol, out var leftType, out _);
        var rightIsNaN = TryGetNaNType(binaryOperation.RightOperand, halfTypeSymbol, out var rightType, out _);
        if (!leftIsNaN && !rightIsNaN)
        {
            replacement = null!;
            return false;
        }

        IOperation otherOperand;
        if (nanOperand.Syntax.IsEquivalentTo(binaryOperation.LeftOperand.Syntax))
        {
            otherOperand = binaryOperation.RightOperand;
        }
        else if (nanOperand.Syntax.IsEquivalentTo(binaryOperation.RightOperand.Syntax))
        {
            otherOperand = binaryOperation.LeftOperand;
        }
        else
        {
            replacement = null!;
            return false;
        }

        var nanType = leftType ?? rightType;
        if (nanType is null)
        {
            replacement = null!;
            return false;
        }

        var isNaNInvocation = (ExpressionSyntax)generator.InvocationExpression(
            generator.MemberAccessExpression(generator.TypeExpression(nanType, addImport: true), "IsNaN"),
            otherOperand.Syntax);

        replacement = binaryOperation.OperatorKind == BinaryOperatorKind.NotEquals
            ? SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, SyntaxFactory.ParenthesizedExpression(isNaNInvocation))
            : isNaNInvocation;

        return true;
    }

    private static bool TryGetNaNType(IOperation operation, INamedTypeSymbol? halfTypeSymbol, out ITypeSymbol? typeSymbol, out ExpressionSyntax? expression)
    {
        while (operation is IConversionOperation conversionOperation)
        {
            operation = conversionOperation.Operand;
        }

        if (operation is IMemberReferenceOperation memberReference &&
            memberReference.Member is ISymbol { Name: "NaN", ContainingType: { } containingType })
        {
            if (containingType.SpecialType is SpecialType.System_Double or SpecialType.System_Single)
            {
                typeSymbol = containingType;
                expression = memberReference.Syntax as ExpressionSyntax;
                return true;
            }

            if (halfTypeSymbol is not null && containingType.IsEqualTo(halfTypeSymbol))
            {
                typeSymbol = containingType;
                expression = memberReference.Syntax as ExpressionSyntax;
                return true;
            }
        }

        typeSymbol = null;
        expression = null;
        return false;
    }
}

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UsePatternMatchingInsteadOfHasvalueFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(RuleIdentifiers.UsePatternMatchingInsteadOfHasvalue);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not MemberAccessExpressionSyntax memberAccess)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use pattern matching",
                ct => Update(context.Document, memberAccess, ct),
                equivalenceKey: "Use pattern matching"),
            context.Diagnostics);
    }

    private static async Task<Document> Update(Document document, MemberAccessExpressionSyntax node, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (editor.SemanticModel.GetOperation(node, cancellationToken) is not IPropertyReferenceOperation operation)
            return document;

        var (nodeToReplace, negate) = GetNodeToReplace(operation);
        var target = operation.Instance?.Syntax as ExpressionSyntax;
        var newNode = MakeIsNotNull(target ?? node, negate);
        editor.ReplaceNode(nodeToReplace, newNode);
        return editor.GetChangedDocument();
    }

    private static (SyntaxNode Node, bool Negate) GetNodeToReplace(IOperation operation)
    {
        if (operation.Parent is IUnaryOperation unaryOperation && unaryOperation.OperatorKind == UnaryOperatorKind.Not)
            return (operation.Parent.Syntax, true);

        if (operation.Parent is IBinaryOperation binaryOperation &&
            (binaryOperation.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals))
        {
            if (binaryOperation.RightOperand.ConstantValue is { HasValue: true, Value: bool rightValue })
            {
                var negate = (!rightValue && binaryOperation.OperatorKind is BinaryOperatorKind.Equals) ||
                             (rightValue && binaryOperation.OperatorKind is BinaryOperatorKind.NotEquals);
                return (operation.Parent.Syntax, negate);
            }

            if (binaryOperation.LeftOperand.ConstantValue is { HasValue: true, Value: bool leftValue })
            {
                var negate = (!leftValue && binaryOperation.OperatorKind is BinaryOperatorKind.Equals) ||
                             (leftValue && binaryOperation.OperatorKind is BinaryOperatorKind.NotEquals);
                return (operation.Parent.Syntax, negate);
            }
        }

        if (operation.Parent is IIsPatternOperation { Pattern: IConstantPatternOperation { Value: ILiteralOperation { ConstantValue.Value: bool value } } })
        {
            if (value)
            {
                return (operation.Parent.Syntax, false);
            }
            else
            {
                return (operation.Parent.Syntax, true);
            }
        }

        return (operation.Syntax, false);
    }

    private static IsPatternExpressionSyntax MakeIsNotNull(ExpressionSyntax instance, bool negate)
    {
        PatternSyntax constantExpression = ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression));
        if (!negate)
        {
            constantExpression = UnaryPattern(constantExpression);
        }

        return IsPatternExpression(ParenthesizedExpression(instance).WithAdditionalAnnotations(Simplifier.Annotation), constantExpression);
    }
}

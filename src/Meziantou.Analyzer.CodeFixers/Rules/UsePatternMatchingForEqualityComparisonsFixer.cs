using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
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
public sealed class UsePatternMatchingForEqualityComparisonsFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(RuleIdentifiers.UsePatternMatchingForNullCheck, RuleIdentifiers.UsePatternMatchingForNullEquality, RuleIdentifiers.UsePatternMatchingForEqualityComparison, RuleIdentifiers.UsePatternMatchingForInequalityComparison);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not BinaryExpressionSyntax invocation)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use pattern matching",
                ct => Update(context.Document, invocation, ct),
                equivalenceKey: "Use pattern matching"),
            context.Diagnostics);
    }

    private static async Task<Document> Update(Document document, BinaryExpressionSyntax node, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (editor.SemanticModel.GetOperation(node, cancellationToken) is not IBinaryOperation operation)
            return document;

        if (UsePatternMatchingForEqualityComparisonsCommon.IsNull(operation.LeftOperand))
        {
            var newExpression = MakeIsNull(operation, operation.RightOperand);
            if (newExpression is not null)
            {
                editor.ReplaceNode(node, newExpression);
            }
        }
        else if (UsePatternMatchingForEqualityComparisonsCommon.IsNull(operation.RightOperand))
        {
            var newExpression = MakeIsNull(operation, operation.LeftOperand);
            if (newExpression is not null)
            {
                editor.ReplaceNode(node, newExpression);
            }
        }
        else
        {
            var (expression, constant) = UsePatternMatchingForEqualityComparisonsCommon.IsConstantLiteral(operation.RightOperand) ? (operation.LeftOperand, operation.RightOperand) : (operation.RightOperand, operation.LeftOperand);

            PatternSyntax constantExpression = ConstantPattern((ExpressionSyntax)constant.Syntax);
            if (operation.OperatorKind is BinaryOperatorKind.NotEquals)
            {
                constantExpression = UnaryPattern(constantExpression);
            }

            var newExpression = IsPatternExpression(ParenthesizedExpression((ExpressionSyntax)expression.Syntax).WithAdditionalAnnotations(Simplifier.Annotation), constantExpression);
            if (newExpression is not null)
            {
                editor.ReplaceNode(node, newExpression);
            }
        }

        return editor.GetChangedDocument();
    }

    private static IsPatternExpressionSyntax? MakeIsNull(IBinaryOperation binaryOperation, IOperation expressionOperation)
    {
        if (expressionOperation.Syntax is not ExpressionSyntax expression)
            return null;

        PatternSyntax constantExpression = ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression));
        if (binaryOperation.OperatorKind is BinaryOperatorKind.NotEquals)
        {
            constantExpression = UnaryPattern(constantExpression);
        }

        return IsPatternExpression(ParenthesizedExpression(expression).WithAdditionalAnnotations(Simplifier.Annotation), constantExpression);
    }
}

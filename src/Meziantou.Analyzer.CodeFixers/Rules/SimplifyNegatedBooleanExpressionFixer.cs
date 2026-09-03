using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class SimplifyNegatedBooleanExpressionFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.SimplifyNegatedBooleanExpression);

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

        var common = new SimplifyNegatedBooleanExpressionCommon(semanticModel.Compilation);
        if (!TryGetOperationToFix(semanticModel, nodeToFix, common, context.CancellationToken, out _))
            return;

        const string Title = "Simplify boolean expression";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => SimplifyBooleanExpression(context.Document, nodeToFix, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Document> SimplifyBooleanExpression(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var common = new SimplifyNegatedBooleanExpressionCommon(editor.SemanticModel.Compilation);
        if (!TryGetOperationToFix(editor.SemanticModel, nodeToFix, common, cancellationToken, out var operation))
            return document;

        if (!common.TryMatch(operation, out var binaryOperation))
            return document;

        var left = NegateOperand(binaryOperation.LeftOperand, common);
        var right = NegateOperand(binaryOperation.RightOperand, common);
        if (left is null || right is null)
            return document;

        var newExpression = BinaryExpression(
            GetExpressionSyntaxKind(SimplifyNegatedBooleanExpressionCommon.GetOppositeConditionalOperatorKind(binaryOperation.OperatorKind)),
            left.Parenthesize(),
            Token(GetOperatorTokenSyntaxKind(SimplifyNegatedBooleanExpressionCommon.GetOppositeConditionalOperatorKind(binaryOperation.OperatorKind))),
            right.Parenthesize());

        var replacement = ParenthesizedExpression(newExpression)
            .WithTriviaFrom(operation.Syntax)
            .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation);

        editor.ReplaceNode(operation.Syntax, replacement);
        return editor.GetChangedDocument();
    }

    private static ExpressionSyntax? NegateOperand(IOperation operation, SimplifyNegatedBooleanExpressionCommon common)
    {
        operation = SimplifyNegatedBooleanExpressionCommon.Unwrap(operation);
        if (!common.TryGetNegationAction(operation, out var action))
            return null;

        return action switch
        {
            SimplifyNegatedBooleanExpressionCommon.NegationAction.RemoveLogicalNot => RemoveLogicalNot((IUnaryOperation)operation),
            SimplifyNegatedBooleanExpressionCommon.NegationAction.FlipComparison => FlipComparison((IBinaryOperation)operation, common),
            SimplifyNegatedBooleanExpressionCommon.NegationAction.AddLogicalNot => AddLogicalNot(operation),
            _ => null,
        };
    }

    private static ExpressionSyntax? RemoveLogicalNot(IUnaryOperation operation)
    {
        operation = (IUnaryOperation)SimplifyNegatedBooleanExpressionCommon.Unwrap(operation);
        var operand = SimplifyNegatedBooleanExpressionCommon.Unwrap(operation.Operand);
        return operand.Syntax is ExpressionSyntax expression ? expression.WithoutTrivia() : null;
    }

    private static BinaryExpressionSyntax? FlipComparison(IBinaryOperation operation, SimplifyNegatedBooleanExpressionCommon common)
    {
        operation = (IBinaryOperation)SimplifyNegatedBooleanExpressionCommon.Unwrap(operation);
        if (!common.TryGetOppositeComparisonOperatorKind(operation, out var operatorKind))
            return null;

        if (operation.Syntax is not BinaryExpressionSyntax binaryExpression)
            return null;

        // The node must be rebuilt with the kind of the new operator: replacing only the token would keep
        // the kind of the original operator, which no syntax tree of the same code would have
        return BinaryExpression(
            GetExpressionSyntaxKind(operatorKind),
            binaryExpression.Left,
            Token(GetOperatorTokenSyntaxKind(operatorKind)),
            binaryExpression.Right)
            .WithoutLeadingTrivia()
            .WithoutTrailingTrivia();
    }

    private static PrefixUnaryExpressionSyntax? AddLogicalNot(IOperation operation)
    {
        operation = SimplifyNegatedBooleanExpressionCommon.Unwrap(operation);
        if (operation.Syntax is not ExpressionSyntax expression)
            return null;

        return PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, expression.WithoutTrivia().Parenthesize());
    }

    private static bool TryGetOperationToFix(SemanticModel semanticModel, SyntaxNode node, SimplifyNegatedBooleanExpressionCommon common, CancellationToken cancellationToken, out IUnaryOperation operation)
    {
        foreach (var candidate in node.AncestorsAndSelf())
        {
            if (semanticModel.GetOperation(candidate, cancellationToken) is IUnaryOperation unaryOperation &&
                common.TryMatch(unaryOperation, out _))
            {
                operation = unaryOperation;
                return true;
            }
        }

        operation = null!;
        return false;
    }

    private static SyntaxKind GetExpressionSyntaxKind(BinaryOperatorKind operatorKind)
    {
        return operatorKind switch
        {
            BinaryOperatorKind.ConditionalAnd => SyntaxKind.LogicalAndExpression,
            BinaryOperatorKind.ConditionalOr => SyntaxKind.LogicalOrExpression,
            BinaryOperatorKind.Equals => SyntaxKind.EqualsExpression,
            BinaryOperatorKind.NotEquals => SyntaxKind.NotEqualsExpression,
            BinaryOperatorKind.LessThan => SyntaxKind.LessThanExpression,
            BinaryOperatorKind.LessThanOrEqual => SyntaxKind.LessThanOrEqualExpression,
            BinaryOperatorKind.GreaterThan => SyntaxKind.GreaterThanExpression,
            BinaryOperatorKind.GreaterThanOrEqual => SyntaxKind.GreaterThanOrEqualExpression,
            _ => throw new ArgumentOutOfRangeException(nameof(operatorKind)),
        };
    }

    private static SyntaxKind GetOperatorTokenSyntaxKind(BinaryOperatorKind operatorKind)
    {
        return operatorKind switch
        {
            BinaryOperatorKind.ConditionalAnd => SyntaxKind.AmpersandAmpersandToken,
            BinaryOperatorKind.ConditionalOr => SyntaxKind.BarBarToken,
            BinaryOperatorKind.Equals => SyntaxKind.EqualsEqualsToken,
            BinaryOperatorKind.NotEquals => SyntaxKind.ExclamationEqualsToken,
            BinaryOperatorKind.LessThan => SyntaxKind.LessThanToken,
            BinaryOperatorKind.LessThanOrEqual => SyntaxKind.LessThanEqualsToken,
            BinaryOperatorKind.GreaterThan => SyntaxKind.GreaterThanToken,
            BinaryOperatorKind.GreaterThanOrEqual => SyntaxKind.GreaterThanEqualsToken,
            _ => throw new ArgumentOutOfRangeException(nameof(operatorKind)),
        };
    }
}

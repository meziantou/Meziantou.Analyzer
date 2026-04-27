using System;
using System.Collections.Generic;
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
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UsePatternMatchingForEqualityComparisonsFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(RuleIdentifiers.UsePatternMatchingForNullCheck, RuleIdentifiers.UsePatternMatchingForNullEquality, RuleIdentifiers.UsePatternMatchingForEqualityComparison, RuleIdentifiers.UsePatternMatchingForInequalityComparison);

    public override FixAllProvider GetFixAllProvider() => UsePatternMatchingForEqualityComparisonsFixAllProvider.Instance;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not BinaryExpressionSyntax binaryExpression)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (semanticModel.GetOperation(binaryExpression, context.CancellationToken) is not IBinaryOperation)
            return;

        var expressionToFix = GetContainingBooleanExpression(binaryExpression);
        var updatedExpression = RewriteExpression(expressionToFix, semanticModel, context.CancellationToken);
        if (SyntaxFactory.AreEquivalent(expressionToFix, updatedExpression))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use pattern matching",
                ct => UpdateDocumentAsync(context.Document, binaryExpression, ct),
                equivalenceKey: "Use pattern matching"),
            context.Diagnostics);
    }

    internal static async Task<Document> UpdateDocumentAsync(Document document, BinaryExpressionSyntax node, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var expressionToFix = GetContainingBooleanExpression(node);
        var updatedExpression = RewriteExpression(expressionToFix, editor.SemanticModel, cancellationToken);
        if (SyntaxFactory.AreEquivalent(expressionToFix, updatedExpression))
            return document;

        editor.ReplaceNode(expressionToFix, updatedExpression.WithTriviaFrom(expressionToFix));
        return editor.GetChangedDocument();
    }

    private static ExpressionSyntax GetContainingBooleanExpression(BinaryExpressionSyntax binaryExpression)
    {
        ExpressionSyntax current = binaryExpression;
        while (TryGetParentLogicalExpression(current, out var parentBinary))
        {
            current = parentBinary;
        }

        return current;
    }

    private static bool TryGetParentLogicalExpression(ExpressionSyntax expression, out BinaryExpressionSyntax parentBinary)
    {
        var parent = expression.Parent;
        if (parent is ParenthesizedExpressionSyntax parenthesizedExpression)
        {
            parent = parenthesizedExpression.Parent;
        }

        if (parent is BinaryExpressionSyntax binaryExpression && IsLogicalBinary(binaryExpression.Kind()))
        {
            parentBinary = binaryExpression;
            return true;
        }

        parentBinary = null!;
        return false;
    }

    private static ExpressionSyntax RewriteExpression(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (expression is ParenthesizedExpressionSyntax parenthesizedExpression)
        {
            var updatedExpression = RewriteExpression(parenthesizedExpression.Expression, semanticModel, cancellationToken);
            return SyntaxFactory.AreEquivalent(parenthesizedExpression.Expression, updatedExpression) ? expression : parenthesizedExpression.WithExpression(updatedExpression);
        }

        if (expression is BinaryExpressionSyntax binaryExpression)
        {
            if (binaryExpression.IsKind(SyntaxKind.LogicalOrExpression) || binaryExpression.IsKind(SyntaxKind.LogicalAndExpression))
            {
                return RewriteLogicalBinaryExpression(binaryExpression, semanticModel, cancellationToken);
            }

            if (TryCreatePatternExpression(binaryExpression, semanticModel, cancellationToken, out var updatedExpression))
            {
                return updatedExpression;
            }
        }

        return expression;
    }

    private static ExpressionSyntax RewriteLogicalBinaryExpression(BinaryExpressionSyntax rootExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var logicalExpressionKind = rootExpression.Kind();
        var terms = new List<ExpressionSyntax>();
        FlattenLogicalTerms(rootExpression, logicalExpressionKind, terms);

        var expectedComparisonOperatorKind = logicalExpressionKind switch
        {
            SyntaxKind.LogicalOrExpression => BinaryOperatorKind.Equals,
            SyntaxKind.LogicalAndExpression => BinaryOperatorKind.NotEquals,
            _ => throw new InvalidOperationException("Unexpected logical expression kind"),
        };

        var updatedTerms = new List<ExpressionSyntax>(terms.Count);
        var mergeCandidates = new List<DiscreteComparisonCandidate>();
        foreach (var term in terms)
        {
            if (TryCreateDiscreteComparisonCandidate(term, expectedComparisonOperatorKind, semanticModel, cancellationToken, out var candidate))
            {
                if (mergeCandidates.Count > 0 && !SyntaxFactory.AreEquivalent(mergeCandidates[0].Expression, candidate.Expression))
                {
                    updatedTerms.Add(CreatePatternExpressionFromCandidates(mergeCandidates, expectedComparisonOperatorKind));
                    mergeCandidates.Clear();
                }

                mergeCandidates.Add(candidate);
                continue;
            }

            if (mergeCandidates.Count > 0)
            {
                updatedTerms.Add(CreatePatternExpressionFromCandidates(mergeCandidates, expectedComparisonOperatorKind));
                mergeCandidates.Clear();
            }

            updatedTerms.Add(RewriteExpression(term, semanticModel, cancellationToken));
        }

        if (mergeCandidates.Count > 0)
        {
            updatedTerms.Add(CreatePatternExpressionFromCandidates(mergeCandidates, expectedComparisonOperatorKind));
        }

        if (updatedTerms.Count == 0)
            return rootExpression;

        var updatedExpression = updatedTerms[0];
        for (var i = 1; i < updatedTerms.Count; i++)
        {
            updatedExpression = logicalExpressionKind switch
            {
                SyntaxKind.LogicalOrExpression => BinaryExpression(SyntaxKind.LogicalOrExpression, updatedExpression, updatedTerms[i]),
                SyntaxKind.LogicalAndExpression => BinaryExpression(SyntaxKind.LogicalAndExpression, updatedExpression, updatedTerms[i]),
                _ => throw new InvalidOperationException("Unexpected logical expression kind"),
            };
        }

        return updatedExpression;
    }

    private static void FlattenLogicalTerms(ExpressionSyntax expression, SyntaxKind logicalExpressionKind, List<ExpressionSyntax> terms)
    {
        if (expression is BinaryExpressionSyntax binaryExpression && binaryExpression.IsKind(logicalExpressionKind))
        {
            FlattenLogicalTerms(binaryExpression.Left, logicalExpressionKind, terms);
            FlattenLogicalTerms(binaryExpression.Right, logicalExpressionKind, terms);
            return;
        }

        terms.Add(expression);
    }

    private static IsPatternExpressionSyntax CreatePatternExpressionFromCandidates(List<DiscreteComparisonCandidate> candidates, BinaryOperatorKind expectedComparisonOperatorKind)
    {
        PatternSyntax combinedPattern = candidates[0].Pattern;
        for (var i = 1; i < candidates.Count; i++)
        {
            combinedPattern = BinaryPattern(SyntaxKind.OrPattern, combinedPattern, Token(SyntaxKind.OrKeyword), candidates[i].Pattern);
        }

        if (expectedComparisonOperatorKind is BinaryOperatorKind.NotEquals)
        {
            combinedPattern = candidates.Count > 1 ? UnaryPattern(ParenthesizedPattern(combinedPattern)) : UnaryPattern(combinedPattern);
        }

        return IsPatternExpression(candidates[0].Expression.Parentheses(), combinedPattern);
    }

    private static bool TryCreateDiscreteComparisonCandidate(ExpressionSyntax expression, BinaryOperatorKind expectedComparisonOperatorKind, SemanticModel semanticModel, CancellationToken cancellationToken, out DiscreteComparisonCandidate candidate)
    {
        candidate = default;

        if (semanticModel.GetOperation(expression, cancellationToken) is not IBinaryOperation operation)
            return false;

        if (operation is not { OperatorMethod: null } || operation.OperatorKind != expectedComparisonOperatorKind)
            return false;

        var leftIsConstant = UsePatternMatchingForEqualityComparisonsCommon.IsConstantLiteral(operation.LeftOperand);
        var rightIsConstant = UsePatternMatchingForEqualityComparisonsCommon.IsConstantLiteral(operation.RightOperand);
        if (!(leftIsConstant ^ rightIsConstant))
            return false;

        var constantOperation = leftIsConstant ? operation.LeftOperand : operation.RightOperand;
        if (UsePatternMatchingForEqualityComparisonsCommon.IsNull(constantOperation))
            return false;

        var expressionOperation = leftIsConstant ? operation.RightOperand : operation.LeftOperand;
        if (expressionOperation.Syntax is not ExpressionSyntax valueExpression || constantOperation.Syntax is not ExpressionSyntax constantExpression)
            return false;

        candidate = new(valueExpression, ConstantPattern(constantExpression));
        return true;
    }

    private static bool TryCreatePatternExpression(BinaryExpressionSyntax binaryExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out IsPatternExpressionSyntax updatedExpression)
    {
        updatedExpression = null!;
        if (semanticModel.GetOperation(binaryExpression, cancellationToken) is not IBinaryOperation operation)
            return false;

        if (operation is not { OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals, OperatorMethod: null })
            return false;

        if (UsePatternMatchingForEqualityComparisonsCommon.IsNull(operation.LeftOperand))
        {
            return TryCreateNullPatternExpression(operation, operation.RightOperand, out updatedExpression);
        }

        if (UsePatternMatchingForEqualityComparisonsCommon.IsNull(operation.RightOperand))
        {
            return TryCreateNullPatternExpression(operation, operation.LeftOperand, out updatedExpression);
        }

        var leftIsConstant = UsePatternMatchingForEqualityComparisonsCommon.IsConstantLiteral(operation.LeftOperand);
        var rightIsConstant = UsePatternMatchingForEqualityComparisonsCommon.IsConstantLiteral(operation.RightOperand);
        if (!(leftIsConstant ^ rightIsConstant))
            return false;

        var constantOperation = leftIsConstant ? operation.LeftOperand : operation.RightOperand;
        var expressionOperation = leftIsConstant ? operation.RightOperand : operation.LeftOperand;

        if (constantOperation.Syntax is not ExpressionSyntax constantExpression || expressionOperation.Syntax is not ExpressionSyntax valueExpression)
            return false;

        PatternSyntax constantPattern = ConstantPattern(constantExpression);
        if (operation.OperatorKind is BinaryOperatorKind.NotEquals)
        {
            constantPattern = UnaryPattern(constantPattern);
        }

        updatedExpression = IsPatternExpression(valueExpression.Parentheses(), constantPattern);
        return true;
    }

    private static bool TryCreateNullPatternExpression(IBinaryOperation binaryOperation, IOperation expressionOperation, out IsPatternExpressionSyntax updatedExpression)
    {
        updatedExpression = null!;
        if (expressionOperation.Syntax is not ExpressionSyntax expression)
            return false;

        PatternSyntax constantPattern = ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression));
        if (binaryOperation.OperatorKind is BinaryOperatorKind.NotEquals)
        {
            constantPattern = UnaryPattern(constantPattern);
        }

        updatedExpression = IsPatternExpression(expression.Parentheses(), constantPattern);
        return true;
    }

    private static bool IsLogicalBinary(SyntaxKind kind) => kind is SyntaxKind.LogicalAndExpression or SyntaxKind.LogicalOrExpression;

    private readonly record struct DiscreteComparisonCandidate(ExpressionSyntax Expression, PatternSyntax Pattern);
}

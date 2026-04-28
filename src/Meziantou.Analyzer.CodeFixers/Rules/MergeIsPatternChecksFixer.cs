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
public sealed class MergeIsPatternChecksFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.MergeIsPatternChecks);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not BinaryExpressionSyntax binaryExpression)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var expressionToFix = GetContainingLogicalExpression(binaryExpression);
        var updatedExpression = RewriteExpression(expressionToFix, semanticModel, context.CancellationToken);
        if (AreEquivalent(expressionToFix, updatedExpression))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Merge is expressions",
                ct => UpdateDocumentAsync(context.Document, binaryExpression, ct),
                equivalenceKey: "Merge is expressions"),
            context.Diagnostics);
    }

    internal static async Task<Document> UpdateDocumentAsync(Document document, BinaryExpressionSyntax node, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var expressionToFix = GetContainingLogicalExpression(node);
        var updatedExpression = RewriteExpression(expressionToFix, editor.SemanticModel, cancellationToken);
        if (AreEquivalent(expressionToFix, updatedExpression))
            return document;

        editor.ReplaceNode(expressionToFix, updatedExpression.WithTriviaFrom(expressionToFix));
        return editor.GetChangedDocument();
    }

    private static ExpressionSyntax GetContainingLogicalExpression(BinaryExpressionSyntax binaryExpression)
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
            return AreEquivalent(parenthesizedExpression.Expression, updatedExpression) ? expression : parenthesizedExpression.WithExpression(updatedExpression);
        }

        if (expression is BinaryExpressionSyntax binaryExpression && IsLogicalBinary(binaryExpression.Kind()))
        {
            return RewriteLogicalBinaryExpression(binaryExpression, semanticModel, cancellationToken);
        }

        return expression;
    }

    private static ExpressionSyntax RewriteLogicalBinaryExpression(BinaryExpressionSyntax rootExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var logicalExpressionKind = rootExpression.Kind();
        var terms = new List<ExpressionSyntax>();
        FlattenLogicalTerms(rootExpression, logicalExpressionKind, terms);

        var mergeCandidates = new List<MergeCandidate>();
        var updatedTerms = new List<ExpressionSyntax>(terms.Count);
        foreach (var term in terms)
        {
            if (TryCreateMergeCandidate(term, semanticModel, cancellationToken, out var candidate))
            {
                if (mergeCandidates.Count == 0 || AreSameMergeTarget(mergeCandidates[0].Target, candidate.Target))
                {
                    mergeCandidates.Add(candidate);
                }
                else
                {
                    FlushCandidates(logicalExpressionKind, mergeCandidates, updatedTerms, semanticModel, cancellationToken);
                    mergeCandidates.Add(candidate);
                }

                continue;
            }

            FlushCandidates(logicalExpressionKind, mergeCandidates, updatedTerms, semanticModel, cancellationToken);
            updatedTerms.Add(RewriteExpression(term, semanticModel, cancellationToken));
        }

        FlushCandidates(logicalExpressionKind, mergeCandidates, updatedTerms, semanticModel, cancellationToken);

        if (updatedTerms.Count == 0)
            return rootExpression;

        var updatedExpression = updatedTerms[0];
        for (var i = 1; i < updatedTerms.Count; i++)
        {
            updatedExpression = logicalExpressionKind switch
            {
                SyntaxKind.LogicalAndExpression => BinaryExpression(SyntaxKind.LogicalAndExpression, updatedExpression, updatedTerms[i]),
                SyntaxKind.LogicalOrExpression => BinaryExpression(SyntaxKind.LogicalOrExpression, updatedExpression, updatedTerms[i]),
                _ => throw new InvalidOperationException("Unexpected logical expression kind"),
            };
        }

        return updatedExpression;
    }

    private static void FlushCandidates(SyntaxKind logicalExpressionKind, List<MergeCandidate> mergeCandidates, List<ExpressionSyntax> updatedTerms, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (mergeCandidates.Count == 0)
            return;

        if (mergeCandidates.Count == 1)
        {
            updatedTerms.Add(RewriteExpression(mergeCandidates[0].TermExpression, semanticModel, cancellationToken));
        }
        else if (CanMergeCandidates(logicalExpressionKind, mergeCandidates))
        {
            updatedTerms.Add(CreateMergedPatternExpression(logicalExpressionKind, mergeCandidates));
        }
        else
        {
            foreach (var candidate in mergeCandidates)
            {
                updatedTerms.Add(RewriteExpression(candidate.TermExpression, semanticModel, cancellationToken));
            }
        }

        mergeCandidates.Clear();
    }

    private static IsPatternExpressionSyntax CreateMergedPatternExpression(SyntaxKind logicalExpressionKind, List<MergeCandidate> mergeCandidates)
    {
        var binaryPatternKind = logicalExpressionKind switch
        {
            SyntaxKind.LogicalAndExpression => SyntaxKind.AndPattern,
            SyntaxKind.LogicalOrExpression => SyntaxKind.OrPattern,
            _ => throw new InvalidOperationException("Unexpected logical expression kind"),
        };

        var operatorTokenKind = logicalExpressionKind switch
        {
            SyntaxKind.LogicalAndExpression => SyntaxKind.AndKeyword,
            SyntaxKind.LogicalOrExpression => SyntaxKind.OrKeyword,
            _ => throw new InvalidOperationException("Unexpected logical expression kind"),
        };

        PatternSyntax mergedPattern = mergeCandidates[0].Pattern;
        for (var i = 1; i < mergeCandidates.Count; i++)
        {
            mergedPattern = BinaryPattern(
                binaryPatternKind,
                ParenthesizePatternIfNeeded(mergedPattern, binaryPatternKind),
                Token(operatorTokenKind),
                ParenthesizePatternIfNeeded(mergeCandidates[i].Pattern, binaryPatternKind));
        }

        return IsPatternExpression(mergeCandidates[0].Expression.Parentheses(), mergedPattern);
    }

    private static PatternSyntax ParenthesizePatternIfNeeded(PatternSyntax pattern, SyntaxKind parentPatternKind)
    {
        if (pattern is ParenthesizedPatternSyntax)
            return pattern;

        if (pattern is BinaryPatternSyntax binaryPattern &&
            parentPatternKind is SyntaxKind.AndPattern &&
            binaryPattern.Kind() is SyntaxKind.OrPattern)
        {
            return ParenthesizedPattern(pattern);
        }

        return pattern;
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

    private static bool TryCreateMergeCandidate(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out MergeCandidate candidate)
    {
        candidate = default;

        var operation = semanticModel.GetOperation(expression, cancellationToken);
        if (operation is null)
            return false;

        operation = UnwrapOperation(operation);
        if (operation is not IIsPatternOperation isPatternOperation)
            return false;

        if (isPatternOperation.Value.Syntax is not ExpressionSyntax valueExpression)
            return false;

        if (!TryGetMergeTarget(isPatternOperation.Value, out var mergeTarget))
            return false;

        if (!TryCreatePatternSyntax(isPatternOperation.Pattern, out var patternSyntax))
            return false;

        candidate = new(expression, mergeTarget, UnwrapParentheses(valueExpression), patternSyntax);
        return true;
    }

    private static IOperation UnwrapOperation(IOperation operation)
    {
        operation = operation.UnwrapConversionOperations();
        while (operation is IParenthesizedOperation parenthesizedOperation)
        {
            operation = parenthesizedOperation.Operand.UnwrapConversionOperations();
        }

        return operation;
    }

    private static bool TryCreatePatternSyntax(IPatternOperation patternOperation, out PatternSyntax patternSyntax)
    {
        switch (patternOperation)
        {
            case IConstantPatternOperation constantPatternOperation:
                if (constantPatternOperation.Syntax is PatternSyntax syntaxPattern)
                {
                    patternSyntax = syntaxPattern;
                    return true;
                }

                if (constantPatternOperation.Value?.Syntax is ExpressionSyntax expressionSyntax)
                {
                    patternSyntax = ConstantPattern(expressionSyntax);
                    return true;
                }

                break;

            case INegatedPatternOperation negatedPatternOperation:
                if (TryCreatePatternSyntax(negatedPatternOperation.Pattern, out var negatedPatternSyntax))
                {
                    patternSyntax = UnaryPattern(
                        negatedPatternSyntax is BinaryPatternSyntax
                            ? ParenthesizedPattern(negatedPatternSyntax)
                            : negatedPatternSyntax);
                    return true;
                }

                break;

            case IBinaryPatternOperation binaryPatternOperation:
                if (TryCreatePatternSyntax(binaryPatternOperation.LeftPattern, out var leftPatternSyntax) &&
                    TryCreatePatternSyntax(binaryPatternOperation.RightPattern, out var rightPatternSyntax) &&
                    TryGetPatternOperator(binaryPatternOperation.OperatorKind, out var binaryPatternKind, out var operatorTokenKind))
                {
                    patternSyntax = BinaryPattern(
                        binaryPatternKind,
                        ParenthesizePatternIfNeeded(leftPatternSyntax, binaryPatternKind),
                        Token(operatorTokenKind),
                        ParenthesizePatternIfNeeded(rightPatternSyntax, binaryPatternKind));
                    return true;
                }

                break;
        }

        patternSyntax = null!;
        return false;
    }

    private static bool TryGetPatternOperator(BinaryOperatorKind operatorKind, out SyntaxKind binaryPatternKind, out SyntaxKind operatorTokenKind)
    {
        switch (operatorKind)
        {
            case BinaryOperatorKind.And:
                binaryPatternKind = SyntaxKind.AndPattern;
                operatorTokenKind = SyntaxKind.AndKeyword;
                return true;
            case BinaryOperatorKind.Or:
                binaryPatternKind = SyntaxKind.OrPattern;
                operatorTokenKind = SyntaxKind.OrKeyword;
                return true;
            default:
                binaryPatternKind = default;
                operatorTokenKind = default;
                return false;
        }
    }

    private static bool TryGetMergeTarget(IOperation operation, out MergeTarget mergeTarget)
    {
        operation = UnwrapOperation(operation);
        switch (operation)
        {
            case ILocalReferenceOperation localReferenceOperation:
                mergeTarget = new(localReferenceOperation.Local);
                return true;
            case IParameterReferenceOperation parameterReferenceOperation:
                mergeTarget = new(parameterReferenceOperation.Parameter);
                return true;
            case IFieldReferenceOperation fieldReferenceOperation when TryGetOptionalMergeTarget(fieldReferenceOperation.Instance, out var fieldInstance):
                mergeTarget = new(fieldReferenceOperation.Field, fieldInstance);
                return true;
            case IPropertyReferenceOperation propertyReferenceOperation when TryGetOptionalMergeTarget(propertyReferenceOperation.Instance, out var propertyInstance):
                mergeTarget = new(propertyReferenceOperation.Property, propertyInstance);
                return true;
            case IEventReferenceOperation eventReferenceOperation when TryGetOptionalMergeTarget(eventReferenceOperation.Instance, out var eventInstance):
                mergeTarget = new(eventReferenceOperation.Event, eventInstance);
                return true;
            case IInstanceReferenceOperation instanceReferenceOperation when instanceReferenceOperation.Type is not null:
                mergeTarget = new(instanceReferenceOperation.Type);
                return true;
            default:
                mergeTarget = null!;
                return false;
        }
    }

    private static bool TryGetOptionalMergeTarget(IOperation? operation, out MergeTarget? mergeTarget)
    {
        if (operation is null)
        {
            mergeTarget = null;
            return true;
        }

        if (TryGetMergeTarget(operation, out var target))
        {
            mergeTarget = target;
            return true;
        }

        mergeTarget = null;
        return false;
    }

    private static bool AreSameMergeTarget(MergeTarget left, MergeTarget right)
    {
        return SymbolEqualityComparer.Default.Equals(left.Symbol, right.Symbol) &&
               AreSameOptionalMergeTarget(left.Instance, right.Instance);
    }

    private static bool AreSameOptionalMergeTarget(MergeTarget? left, MergeTarget? right)
    {
        if (left is null)
            return right is null;

        if (right is null)
            return false;

        return AreSameMergeTarget(left, right);
    }

    private static bool CanMergeCandidates(SyntaxKind logicalExpressionKind, List<MergeCandidate> mergeCandidates)
    {
        if (logicalExpressionKind is not SyntaxKind.LogicalAndExpression)
            return true;

        foreach (var candidate in mergeCandidates)
        {
            if (!IsPositiveConstantPattern(candidate.Pattern))
                return true;
        }

        return false;
    }

    private static bool IsPositiveConstantPattern(PatternSyntax pattern)
    {
        return pattern switch
        {
            ConstantPatternSyntax => true,
            ParenthesizedPatternSyntax parenthesizedPattern => IsPositiveConstantPattern(parenthesizedPattern.Pattern),
            _ => false,
        };
    }

    private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesizedExpression)
        {
            expression = parenthesizedExpression.Expression;
        }

        return expression;
    }

    private static bool IsLogicalBinary(SyntaxKind kind) => kind is SyntaxKind.LogicalAndExpression or SyntaxKind.LogicalOrExpression;

    private sealed record class MergeTarget(ISymbol Symbol, MergeTarget? Instance = null);

    private readonly record struct MergeCandidate(ExpressionSyntax TermExpression, MergeTarget Target, ExpressionSyntax Expression, PatternSyntax Pattern);
}

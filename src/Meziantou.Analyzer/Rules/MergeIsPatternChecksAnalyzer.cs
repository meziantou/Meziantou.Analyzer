using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MergeIsPatternChecksAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.MergeIsPatternChecks,
        title: "Merge is expressions on the same value",
        messageFormat: "Merge is expressions on the same value",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MergeIsPatternChecks));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            if (context.Compilation.GetCSharpLanguageVersion() < LanguageVersion.CSharp9)
                return;

            context.RegisterSyntaxNodeAction(AnalyzeBinary, SyntaxKind.LogicalAndExpression, SyntaxKind.LogicalOrExpression);
        });
    }

    private static void AnalyzeBinary(SyntaxNodeAnalysisContext context)
    {
        var operation = context.SemanticModel.GetOperation(context.Node, context.CancellationToken) as IBinaryOperation;
        if (operation is null)
            return;

        if (operation.OperatorKind is not (BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr))
            return;

        if (context.Node is not BinaryExpressionSyntax rootExpression)
            return;

        if (rootExpression.Parent is BinaryExpressionSyntax parentExpression && parentExpression.IsKind(rootExpression.Kind()))
            return;

        if (!HasMergeableContiguousCandidates(rootExpression, context.SemanticModel, context.CancellationToken))
            return;

        context.ReportDiagnostic(Rule, rootExpression);
    }

    private static bool HasMergeableContiguousCandidates(BinaryExpressionSyntax rootExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var terms = new List<ExpressionSyntax>();
        FlattenLogicalTerms(rootExpression, rootExpression.Kind(), terms);

        var currentGroup = new List<MergeCandidate>();
        foreach (var term in terms)
        {
            if (TryCreateMergeCandidate(term, semanticModel, cancellationToken, out var candidate))
            {
                if (currentGroup.Count == 0 || SyntaxFactory.AreEquivalent(currentGroup[0].Expression, candidate.Expression))
                {
                    currentGroup.Add(candidate);
                }
                else
                {
                    if (currentGroup.Count > 1)
                        return true;

                    currentGroup.Clear();
                    currentGroup.Add(candidate);
                }
            }
            else
            {
                if (currentGroup.Count > 1)
                    return true;

                currentGroup.Clear();
            }
        }

        return currentGroup.Count > 1;
    }

    private static void FlattenLogicalTerms(ExpressionSyntax expression, SyntaxKind operatorKind, List<ExpressionSyntax> terms)
    {
        if (expression is BinaryExpressionSyntax binaryExpression && binaryExpression.IsKind(operatorKind))
        {
            FlattenLogicalTerms(binaryExpression.Left, operatorKind, terms);
            FlattenLogicalTerms(binaryExpression.Right, operatorKind, terms);
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

        if (!TryCreatePatternSyntax(isPatternOperation.Pattern, out var patternSyntax))
            return false;

        candidate = new(UnwrapParentheses(valueExpression), patternSyntax);
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

    private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesizedExpression)
        {
            expression = parenthesizedExpression.Expression;
        }

        return expression;
    }

    private readonly record struct MergeCandidate(ExpressionSyntax Expression, PatternSyntax Pattern);
}

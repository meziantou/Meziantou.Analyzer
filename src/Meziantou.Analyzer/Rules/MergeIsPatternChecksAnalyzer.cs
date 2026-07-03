using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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
                if (currentGroup.Count == 0 || MergeIsPatternChecksCommon.AreSameMergeTarget(currentGroup[0].Target, candidate.Target))
                {
                    currentGroup.Add(candidate);
                }
                else
                {
                    if (CanMergeCandidates(rootExpression.Kind(), currentGroup))
                        return true;

                    currentGroup.Clear();
                    currentGroup.Add(candidate);
                }
            }
            else
            {
                if (CanMergeCandidates(rootExpression.Kind(), currentGroup))
                    return true;

                currentGroup.Clear();
            }
        }

        return CanMergeCandidates(rootExpression.Kind(), currentGroup);
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

        if (!MergeIsPatternChecksCommon.TryGetMergeTarget(isPatternOperation.Value, out var mergeTarget))
            return false;

        var patternSyntax = isPatternOperation.Pattern.Syntax as PatternSyntax;
        candidate = new(mergeTarget, patternSyntax);
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

    private static bool CanMergeCandidates(SyntaxKind logicalExpressionKind, List<MergeCandidate> mergeCandidates)
    {
        if (mergeCandidates.Count <= 1)
            return false;

        foreach (var candidate in mergeCandidates)
        {
            if (candidate.Pattern is not null && ContainsNotPatternWithVariableDesignation(candidate.Pattern))
                return false;
        }

        if (logicalExpressionKind is SyntaxKind.LogicalOrExpression)
        {
            foreach (var candidate in mergeCandidates)
            {
                if (candidate.Pattern is not null && ContainsVariableDesignation(candidate.Pattern))
                    return false;
            }
        }

        return true;
    }

    private static bool ContainsNotPatternWithVariableDesignation(PatternSyntax pattern)
    {
        if (pattern is UnaryPatternSyntax unaryPattern && ContainsVariableDesignation(unaryPattern.Pattern))
            return true;

        foreach (var child in pattern.ChildNodes())
        {
            if (child is PatternSyntax childPattern && ContainsNotPatternWithVariableDesignation(childPattern))
                return true;
        }

        return false;
    }

    private static bool ContainsVariableDesignation(SyntaxNode node)
    {
        if (node is SingleVariableDesignationSyntax)
            return true;

        foreach (var child in node.ChildNodes())
        {
            if (ContainsVariableDesignation(child))
                return true;
        }

        return false;
    }

    private readonly record struct MergeCandidate(MergeIsPatternChecksCommon.MergeTarget Target, PatternSyntax? Pattern);
}

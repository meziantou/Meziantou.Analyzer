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
                if (currentGroup.Count == 0 || AreSameMergeTarget(currentGroup[0].Target, candidate.Target))
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

        if (!TryGetMergeTarget(isPatternOperation.Value, out var mergeTarget))
            return false;

        candidate = new(mergeTarget);
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

    private sealed record class MergeTarget(ISymbol Symbol, MergeTarget? Instance = null);

    private readonly record struct MergeCandidate(MergeTarget Target);
}

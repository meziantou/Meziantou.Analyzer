using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UsePatternMatchingForNullCheckAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_ruleEqual = new(
        RuleIdentifiers.UsePatternMatchingForNullEquality,
        title: "Use pattern matching instead of equality operators",
        messageFormat: "Use pattern matching instead of equality operators",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UsePatternMatchingForNullEquality));

    private static readonly DiagnosticDescriptor s_ruleNotEqual = new(
        RuleIdentifiers.UsePatternMatchingForNullCheck,
        title: "Use pattern matching instead of inequality operators",
        messageFormat: "Use pattern matching instead of inequality operators",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UsePatternMatchingForNullCheck));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_ruleEqual, s_ruleNotEqual);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterOperationAction(AnalyzeBinary, OperationKind.Binary);
    }

    private void AnalyzeBinary(OperationAnalysisContext context)
    {
        var operation = (IBinaryOperation)context.Operation;
        if (operation is { OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals, OperatorMethod: null })
        {
            var leftIfNull = IsNull(operation.LeftOperand);
            var rightIfNull = IsNull(operation.RightOperand);
            if (leftIfNull ^ rightIfNull)
            {
                context.ReportDiagnostic(operation.OperatorKind is BinaryOperatorKind.Equals ? s_ruleEqual : s_ruleNotEqual, operation);
            }
        }
    }

    private static bool IsNull(IOperation operation)
        => operation.UnwrapConversionOperations() is ILiteralOperation { ConstantValue: { HasValue: true, Value: null } };
}

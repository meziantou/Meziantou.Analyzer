using System.Collections.Immutable;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UsePatternMatchingForEqualityComparisonsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor RuleEqualNull = new(
        RuleIdentifiers.UsePatternMatchingForNullEquality,
        title: "Use pattern matching instead of equality operators for null check",
        messageFormat: "Use pattern matching instead of equality operators for null check",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UsePatternMatchingForNullEquality));

    private static readonly DiagnosticDescriptor RuleNotEqualNull = new(
        RuleIdentifiers.UsePatternMatchingForNullCheck,
        title: "Use pattern matching instead of inequality operators for null check",
        messageFormat: "Use pattern matching instead of inequality operators for null check",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UsePatternMatchingForNullCheck));

    private static readonly DiagnosticDescriptor RuleEqualConstant = new(
        RuleIdentifiers.UsePatternMatchingForEqualityComparison,
        title: "Use pattern matching instead of equality operators for discrete value",
        messageFormat: "Use pattern matching instead of equality operators for discrete value",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UsePatternMatchingForEqualityComparison));

    private static readonly DiagnosticDescriptor RuleNotEqualConstant = new(
        RuleIdentifiers.UsePatternMatchingForInequalityComparison,
        title: "Use pattern matching instead of inequality operators for discrete value",
        messageFormat: "Use pattern matching instead of inequality operators for discrete values",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UsePatternMatchingForInequalityComparison));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleEqualNull, RuleNotEqualNull, RuleEqualConstant, RuleNotEqualConstant);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var tree = context.Compilation.SyntaxTrees.FirstOrDefault();
            if (tree is null)
                return;

            if (tree.GetCSharpLanguageVersion() < Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8)
                return;

            var analyzerContext = new AnalyzerContext(context.Compilation);
            context.RegisterOperationAction(analyzerContext.AnalyzeBinary, OperationKind.Binary);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly OperationUtilities _operationUtilities = new(compilation);

        public void AnalyzeBinary(OperationAnalysisContext context)
        {
            var operation = (IBinaryOperation)context.Operation;
            if (operation is { OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals, OperatorMethod: null })
            {
                var leftIsNull = UsePatternMatchingForEqualityComparisonsCommon.IsNull(operation.LeftOperand);
                var rightIsNull = UsePatternMatchingForEqualityComparisonsCommon.IsNull(operation.RightOperand);
                if (leftIsNull ^ rightIsNull)
                {
                    if (_operationUtilities.IsInExpressionContext(operation))
                        return;

                    context.ReportDiagnostic(operation.OperatorKind is BinaryOperatorKind.Equals ? RuleEqualNull : RuleNotEqualNull, operation);
                }
                else if (!leftIsNull && !rightIsNull)
                {
                    var leftIsConstant = UsePatternMatchingForEqualityComparisonsCommon.IsConstantLiteral(operation.LeftOperand);
                    var rightIsConstant = UsePatternMatchingForEqualityComparisonsCommon.IsConstantLiteral(operation.RightOperand);
                    if (leftIsConstant ^ rightIsConstant)
                    {
                        if (_operationUtilities.IsInExpressionContext(operation))
                            return;

                        context.ReportDiagnostic(operation.OperatorKind is BinaryOperatorKind.Equals ? RuleEqualConstant : RuleNotEqualConstant, operation);
                    }
                }
            }
        }
    }
}

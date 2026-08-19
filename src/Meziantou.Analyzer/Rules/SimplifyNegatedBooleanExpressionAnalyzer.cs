using System.Collections.Immutable;
using Meziantou.Framework.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SimplifyNegatedBooleanExpressionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.SimplifyNegatedBooleanExpression,
        title: "Simplify negated boolean expression",
        messageFormat: "Simplify negated boolean expression",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SimplifyNegatedBooleanExpression));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var common = new SimplifyNegatedBooleanExpressionCommon(compilationContext.Compilation);
            compilationContext.RegisterOperationAction(context => AnalyzeUnaryOperation(context, common), OperationKind.Unary);
        });
    }

    private static void AnalyzeUnaryOperation(OperationAnalysisContext context, SimplifyNegatedBooleanExpressionCommon common)
    {
        var operation = (IUnaryOperation)context.Operation;
        if (common.TryMatch(operation, out _))
        {
            context.ReportDiagnostic(Rule, operation);
        }
    }
}

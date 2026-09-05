namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseExclusiveOrOperatorAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseExclusiveOrOperator,
        title: "Use exclusive or operator",
        messageFormat: "Use exclusive or operator",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseExclusiveOrOperator));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterOperationAction(AnalyzeBinaryOperation, OperationKind.Binary);
    }

    private static void AnalyzeBinaryOperation(OperationAnalysisContext context)
    {
        var operation = (IBinaryOperation)context.Operation;
        if (UseExclusiveOrOperatorCommon.TryMatch(operation, out _, out _))
        {
            context.ReportDiagnostic(Rule, operation);
        }
    }
}

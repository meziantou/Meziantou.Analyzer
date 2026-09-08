namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotThrowFromFinallyBlockAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotThrowFromFinallyBlock,
        title: "Do not throw from a finally block",
        messageFormat: "Do not throw from a finally block",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotThrowFromFinallyBlock));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeThrow, OperationKind.Throw);
    }

    private static void AnalyzeThrow(OperationAnalysisContext context)
    {
        var operation = context.Operation;
        var child = operation;
        for (var parent = operation.Parent; parent is not null; child = parent, parent = parent.Parent)
        {
            // A lambda or a local function declared in a finally block is not executed by the finally block
            if (parent is IAnonymousFunctionOperation or ILocalFunctionOperation)
                return;

            if (parent is ITryOperation tryOperation && tryOperation.Finally == child)
            {
                context.ReportDiagnostic(Rule, operation);
                return;
            }
        }
    }
}

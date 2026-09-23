namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotThrowFromFinalizerAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotThrowFromFinalizer,
        title: "Do not throw from a finalizer",
        messageFormat: "Do not throw from a finalizer",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotThrowFromFinalizer));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeThrow, OperationKind.Throw);
    }

    private static void AnalyzeThrow(OperationAnalysisContext context)
    {
        if (context.ContainingSymbol is not IMethodSymbol { MethodKind: MethodKind.Destructor })
            return;

        var operation = (IThrowOperation)context.Operation;
        var exceptionType = operation.GetThrownExceptionType();
        IOperation child = operation;
        for (var parent = operation.Parent; parent is not null; child = parent, parent = parent.Parent)
        {
            // A lambda or a local function declared in a finalizer is not executed by the finalizer
            if (parent is IAnonymousFunctionOperation or ILocalFunctionOperation)
                return;

            // The exception does not escape the finalizer when a try statement declared in the finalizer catches it
            if (parent is ITryOperation tryOperation && tryOperation.Body == child && tryOperation.AlwaysCatches(exceptionType, context.Compilation))
                return;
        }

        context.ReportDiagnostic(Rule, operation);
    }
}

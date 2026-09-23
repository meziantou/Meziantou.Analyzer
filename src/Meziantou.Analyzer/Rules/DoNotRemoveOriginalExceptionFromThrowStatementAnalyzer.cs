namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotRemoveOriginalExceptionFromThrowStatementAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotRemoveOriginalExceptionFromThrowStatement,
        title: "Prefer rethrowing an exception implicitly",
        messageFormat: "Prefer rethrowing an exception implicitly",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotRemoveOriginalExceptionFromThrowStatement));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(Analyze, OperationKind.Throw);
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        var operation = (IThrowOperation)context.Operation;
        if (operation.Exception is null)
            return;

        if (operation.Exception.UnwrapImplicitConversions() is not ILocalReferenceOperation localReferenceOperation)
            return;

        IOperation child = operation;
        foreach (var ancestor in operation.Ancestors())
        {
            switch (ancestor)
            {
                // 'throw;' is not allowed in a lambda or a local function (CS0156), nor in a finally block (CS0724)
                case IAnonymousFunctionOperation or ILocalFunctionOperation:
                case ITryOperation tryOperation when tryOperation.Finally == child:
                    return;

                case ICatchClauseOperation catchOperation:
                    if (catchOperation.Locals.Contains(localReferenceOperation.Local))
                    {
                        context.ReportDiagnostic(Rule, operation);
                    }

                    return;
            }

            child = ancestor;
        }
    }
}

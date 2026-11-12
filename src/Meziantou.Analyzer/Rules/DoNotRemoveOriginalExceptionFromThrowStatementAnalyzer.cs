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

        var catchOperation = GetContainingCatchClause(operation);
        if (catchOperation is null)
            return;

        if (catchOperation.Locals.Contains(localReferenceOperation.Local))
        {
            context.ReportDiagnostic(Rule, operation);
        }
    }

    // "throw;" is only valid in a catch clause.
    // It is not valid in an anonymous function or a local function (CS0156), nor in a finally clause nested in the catch clause (CS0724).
    private static ICatchClauseOperation? GetContainingCatchClause(IOperation operation)
    {
        var current = operation;
        while (current.Parent is { } parent)
        {
            switch (parent)
            {
                case ICatchClauseOperation catchClause:
                    return catchClause;

                case IAnonymousFunctionOperation or ILocalFunctionOperation:
                case ITryOperation tryOperation when tryOperation.Finally == current:
                    return null;
            }

            current = parent;
        }

        return null;
    }
}

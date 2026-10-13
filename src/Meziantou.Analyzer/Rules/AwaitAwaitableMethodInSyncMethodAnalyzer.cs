using Meziantou.Analyzer.Configurations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AwaitAwaitableMethodInSyncMethodAnalyzer : DiagnosticAnalyzer
{
    private static readonly ConfigurationDefinition<bool> ReportDiscardedConfiguration = new(RuleIdentifiers.AwaitAwaitableMethodInSyncMethod + ".report_discarded", defaultValue: false);

    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.AwaitAwaitableMethodInSyncMethod,
        title: "Observe result of async calls",
        messageFormat: "Observe result of async calls",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AwaitAwaitableMethodInSyncMethod));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var awaitableTypes = new AwaitableTypes(context.Compilation);
            var operationUtilities = new OperationUtilities(context.Compilation);
            context.RegisterOperationAction(context => AnalyzeOperation(context, operationUtilities, awaitableTypes), OperationKind.Invocation);
        });
    }

    private static void AnalyzeOperation(OperationAnalysisContext context, OperationUtilities operationUtilities, AwaitableTypes awaitableTypes)
    {
        var operation = (IInvocationOperation)context.Operation;

        // This method is called for every invocation, so the checks are ordered from the cheapest to the most expensive.
        // Most invocations are not statements, and most of the others return void
        var parent = operation.Parent;

        // unwrap all IConditionalAccessOperation
        while (parent is IConditionalAccessOperation conditionalAccess)
        {
            parent = conditionalAccess.Parent;
        }

        var isDiscarded = false;
        if (parent is not (null or IBlockOperation or IExpressionStatementOperation))
        {
            if (parent is not ISimpleAssignmentOperation { Target: IDiscardOperation })
                return;

            isDiscarded = true;
        }

        if (operation.Type is null or { SpecialType: SpecialType.System_Void })
            return;

        if (isDiscarded && !context.Options.GetConfigurationValue(operation, ReportDiscardedConfiguration))
            return;

        if (IsInAsyncMethodOrTopLevelStatements(operation, context.ContainingSymbol))
            return; // Already handled by CS4014

        if (operationUtilities.IsInExpressionContext(operation))
            return;

        if (!awaitableTypes.IsAwaitable(operation.Type, operation.SemanticModel!, operation.Syntax.Span.End))
            return;

        context.ReportDiagnostic(Rule, operation);
    }

    private static bool IsInAsyncMethodOrTopLevelStatements(IOperation operation, ISymbol containingSymbol)
    {
        // The containing symbol is the member, so the operations are used to find the enclosing lambda or local function
        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            switch (parent)
            {
                case IAnonymousFunctionOperation anonymousFunction:
                    return anonymousFunction.Symbol.IsAsync;

                case ILocalFunctionOperation localFunction:
                    return localFunction.Symbol.IsAsync;
            }
        }

        return containingSymbol is IMethodSymbol method && (method.IsAsync || method.IsTopLevelStatementsEntryPointMethod());
    }
}

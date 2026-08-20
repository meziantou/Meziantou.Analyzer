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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var awaitableTypes = new AwaitableTypes(context.Compilation);
            var operationUtilities = new OperationUtilities(context.Compilation);
            context.RegisterSymbolStartAction(context =>
            {
                context.RegisterOperationAction(context => AnalyzeOperation(context, operationUtilities, awaitableTypes), OperationKind.Invocation);
            }, SymbolKind.Method);
        });
    }

    private static void AnalyzeOperation(OperationAnalysisContext context, OperationUtilities operationUtilities, AwaitableTypes awaitableTypes)
    {
        var operation = (IInvocationOperation)context.Operation;
        var reportDiscarded = context.Options.GetConfigurationValue(operation, ReportDiscardedConfiguration);

        var parent = operation.Parent;

        // unwrap all IConditionalAccessOperation
        while (parent is IConditionalAccessOperation conditionalAccess)
        {
            parent = conditionalAccess.Parent;
        }

        if (parent is not (null or IBlockOperation or IExpressionStatementOperation) &&
            (!reportDiscarded || parent is not ISimpleAssignmentOperation { Target: IDiscardOperation }))
            return;

        if (operationUtilities.IsInExpressionContext(operation))
            return;

        var semanticModel = operation.SemanticModel!;
        var position = operation.Syntax.GetLocation().SourceSpan.End;

        var enclosingSymbol = semanticModel.GetEnclosingSymbol(position, context.CancellationToken);
        if (enclosingSymbol is IMethodSymbol method && (method.IsAsync || method.IsTopLevelStatementsEntryPointMethod()))
            return; // Already handled by CS4014

        if (!awaitableTypes.IsAwaitable(operation.Type, semanticModel, position))
            return;

        context.ReportDiagnostic(Rule, operation);
    }
}

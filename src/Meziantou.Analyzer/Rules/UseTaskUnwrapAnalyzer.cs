namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseTaskUnwrapAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseTaskUnwrap,
        title: "Use Unwrap instead of using await twice",
        messageFormat: "Use Unwrap instead of using await twice",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseTaskUnwrap));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var ctx = new AnalyzerContext(context.Compilation);
            if (!ctx.IsValid)
                return;

            context.RegisterOperationAction(ctx.AnalyzeAwait, OperationKind.Await);
        });
    }

    private sealed class AnalyzerContext
    {
        public AnalyzerContext(Compilation compilation)
        {
            TaskSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
            TaskOfTSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");

            ConfiguredTaskAwaitableSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable");
            ConfiguredTaskAwaitableOfTSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1");

            ConfigureAwaitOptionsSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ConfigureAwaitOptions");
            SuppressThrowingValue = ConfigureAwaitOptionsSymbol?.GetMembers("SuppressThrowing").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;

            if (TaskSymbol is not null && TaskOfTSymbol is not null)
            {
                TaskOfTaskSymbol = TaskOfTSymbol.Construct(TaskSymbol);
                TaskOfTaskOfTSymbol = TaskOfTSymbol.Construct(TaskOfTSymbol);
            }
        }

        public INamedTypeSymbol? TaskSymbol { get; }
        public INamedTypeSymbol? TaskOfTSymbol { get; }
        public INamedTypeSymbol? TaskOfTaskSymbol { get; }
        public INamedTypeSymbol? TaskOfTaskOfTSymbol { get; }

        public INamedTypeSymbol? ConfiguredTaskAwaitableSymbol { get; }
        public INamedTypeSymbol? ConfiguredTaskAwaitableOfTSymbol { get; }

        public INamedTypeSymbol? ConfigureAwaitOptionsSymbol { get; }
        public int? SuppressThrowingValue { get; }

        public bool IsValid => TaskOfTaskSymbol is not null || TaskOfTaskOfTSymbol is not null;

        public void AnalyzeAwait(OperationAnalysisContext context)
        {
            var operation = (IAwaitOperation)context.Operation;

            if (operation.Operation is IAwaitOperation childAwaitOperation)
            {
                if (childAwaitOperation.Operation.Type is not INamedTypeSymbol childAwaitOperationType)
                    return;

                // Task<Task>
                if (childAwaitOperationType.IsEqualTo(TaskOfTaskSymbol))
                {
                    context.ReportDiagnostic(Rule, operation);
                }
                // Task<Task<T>>
                else if (childAwaitOperationType.OriginalDefinition.IsEqualTo(TaskOfTSymbol) && childAwaitOperationType.TypeArguments[0].OriginalDefinition.IsEqualTo(TaskOfTSymbol))
                {
                    context.ReportDiagnostic(Rule, operation);
                }
            }
            else if (operation.Operation is IInvocationOperation { Instance: IAwaitOperation { Operation.Type: INamedTypeSymbol childAwaitOperationType } } invocation && invocation.Type.IsEqualToAny(ConfiguredTaskAwaitableSymbol, ConfiguredTaskAwaitableOfTSymbol))
            {
                // The inner task is the only one configured with SuppressThrowing, whereas Unwrap() would also
                // suppress the exceptions of the outer task, which the outer await currently propagates
                if (MaySuppressThrowing(invocation))
                    return;

                // Task<Task>
                if (childAwaitOperationType.IsEqualTo(TaskOfTaskSymbol))
                {
                    context.ReportDiagnostic(Rule, operation);
                }
                // Task<Task<T>>
                else if (childAwaitOperationType.OriginalDefinition.IsEqualTo(TaskOfTSymbol) && childAwaitOperationType.TypeArguments[0].OriginalDefinition.IsEqualTo(TaskOfTSymbol))
                {
                    context.ReportDiagnostic(Rule, operation);
                }
            }
        }

        private bool MaySuppressThrowing(IInvocationOperation operation)
        {
            if (SuppressThrowingValue is not { } suppressThrowing)
                return false;

            foreach (var argument in operation.Arguments)
            {
                if (!argument.Parameter!.Type.IsEqualTo(ConfigureAwaitOptionsSymbol))
                    continue;

                // The options are only known not to suppress the exceptions when they are a constant
                return argument.Value.ConstantValue is not { HasValue: true, Value: int options } || (options & suppressThrowing) is not 0;
            }

            return false;
        }
    }
}

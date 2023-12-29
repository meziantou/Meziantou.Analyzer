using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseTaskUnwrapAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseTaskUnwrap,
        title: "Use Unwrap instead of double await",
        messageFormat: "Use Unwrap instead of double await",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseTaskUnwrap));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

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
            else if (operation.Operation is IInvocationOperation { Instance: IAwaitOperation { Operation.Type: INamedTypeSymbol childAwaitOperationType }, Type: var invocationType } && invocationType.IsEqualToAny(ConfiguredTaskAwaitableSymbol, ConfiguredTaskAwaitableOfTSymbol))
            {
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
    }
}

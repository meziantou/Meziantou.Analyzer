using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReturnTaskInsteadOfAwaitingItAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ReturnTaskInsteadOfAwaitingIt,
        title: "Return the task instead of awaiting it",
        messageFormat: "Return the task instead of awaiting it",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ReturnTaskInsteadOfAwaitingIt));

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
        private readonly Compilation _compilation;
        private readonly AwaitableTypes _awaitableTypes;
        private readonly ITypeSymbol?[] _configuredAwaitableSymbols;

        public AnalyzerContext(Compilation compilation)
        {
            _compilation = compilation;
            _awaitableTypes = new AwaitableTypes(compilation);
            _configuredAwaitableSymbols =
            [
                compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable"),
                compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1"),
                compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable"),
                compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable`1"),
            ];
        }

        public bool IsValid => _awaitableTypes.TaskSymbol is not null;

        public void AnalyzeAwait(OperationAnalysisContext context)
        {
            var awaitOperation = (IAwaitOperation)context.Operation;

            // The await must be the whole body of the enclosing method: "return await X;" or "await X;"
            if (awaitOperation.Parent is not (IReturnOperation or IExpressionStatementOperation))
                return;

            if (awaitOperation.Parent.Parent is not IBlockOperation { Operations.Length: 1 } block)
                return;

            var method = block.Parent switch
            {
                IMethodBodyOperation => context.ContainingSymbol as IMethodSymbol,
                ILocalFunctionOperation localFunction => localFunction.Symbol,
                IAnonymousFunctionOperation anonymousFunction => anonymousFunction.Symbol,
                _ => null,
            };

            if (method is null || !method.IsAsync)
                return;

            if (!_awaitableTypes.IsAsyncBuildableAndNotVoid(method.ReturnType))
                return;

            // Resolve the underlying task, ignoring a trailing ConfigureAwait call
            var taskOperation = awaitOperation.Operation;
            if (taskOperation is IInvocationOperation { Instance: { } instance, Type: var configuredType } &&
                configuredType is not null && configuredType.OriginalDefinition.IsEqualToAny(_configuredAwaitableSymbols))
            {
                taskOperation = instance;
            }

            var taskType = taskOperation.Type;
            if (taskType is null)
                return;

            // The task must be directly assignable to the return type (identity or implicit reference conversion)
            var conversion = _compilation.ClassifyConversion(taskType, method.ReturnType);
            if (!conversion.IsIdentity && !(conversion.IsImplicit && conversion.IsReference))
                return;

            // Removing the await is only valid if the task expression does not itself contain an await
            foreach (var descendant in taskOperation.DescendantsAndSelf())
            {
                if (descendant is IAwaitOperation)
                    return;
            }

            context.ReportDiagnostic(Rule, awaitOperation);
        }
    }
}

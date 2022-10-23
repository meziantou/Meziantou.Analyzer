using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseJSRuntimeInvokeVoidAsyncWhenReturnValueIsNotUsed : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.UseJSRuntimeInvokeVoidAsyncWhenReturnValueIsNotUsed,
        title: "Use InvokeVoidAsync when the returned value is not used",
        messageFormat: "Use '{0}' when the returned value is not used",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseJSRuntimeInvokeVoidAsyncWhenReturnValueIsNotUsed));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.IsValid)
            {
                ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            }
        });
    }

    private sealed class AnalyzerContext
    {
        public AnalyzerContext(Compilation compilation)
        {
            IJSInProcessRuntimeSymbol = compilation.GetBestTypeByMetadataName("Microsoft.JSInterop.IJSRuntime");
            JSRuntimeExtensionsSymbol = compilation.GetBestTypeByMetadataName("Microsoft.JSInterop.JSRuntimeExtensions");
            IJSInProcessRuntimeSymbol = compilation.GetBestTypeByMetadataName("Microsoft.JSInterop.IJSInProcessRuntime");
            JSInProcessRuntimeExtensionsSymbol = compilation.GetBestTypeByMetadataName("Microsoft.JSInterop.JSInProcessRuntimeExtensions");
        }

        public INamedTypeSymbol? IJSRuntimeSymbol { get; }
        public INamedTypeSymbol? JSRuntimeExtensionsSymbol { get; }

        public INamedTypeSymbol? IJSInProcessRuntimeSymbol { get; }
        public INamedTypeSymbol? JSInProcessRuntimeExtensionsSymbol { get; }

        public bool IsValid => IJSInProcessRuntimeSymbol != null || IJSRuntimeSymbol != null;

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            var targetMethod = operation.TargetMethod;

            if (targetMethod.ContainingType.IsEqualToAny(IJSRuntimeSymbol, JSRuntimeExtensionsSymbol))
            {
                if (targetMethod.Name is "InvokeAsync" && !IsValueUsed(operation))
                {
                    context.ReportDiagnostic(s_rule, operation, "InvokeVoidAsync");
                }
            }
            else if (targetMethod.ContainingType.IsEqualToAny(IJSInProcessRuntimeSymbol, JSInProcessRuntimeExtensionsSymbol))
            {
                if (targetMethod.Name is "InvokeAsync" or "Invoke" && !IsValueUsed(operation))
                {
                    context.ReportDiagnostic(s_rule, operation, targetMethod.Name.EndsWith("Async", System.StringComparison.Ordinal) ? "InvokeVoidAsync" : "InvokeVoid");
                }
            }

            static bool IsValueUsed(IInvocationOperation operation)
            {
                var parent = operation.Parent;
                if (parent is IAwaitOperation)
                {
                    parent = parent.Parent;
                }

                if (parent == null || parent is IBlockOperation || parent is IExpressionStatementOperation)
                    return false;

                return true;
            }
        }
    }
}

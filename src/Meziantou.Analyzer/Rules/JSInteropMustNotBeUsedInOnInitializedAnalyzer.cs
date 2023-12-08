using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class JSInteropMustNotBeUsedInOnInitializedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.JSRuntimeMustNotBeUsedInOnInitialized,
        title: "JSRuntime must not be used in OnInitialized or OnInitializedAsync",
        messageFormat: "{0} must not be used in OnInitialized or OnInitializedAsync",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.JSRuntimeMustNotBeUsedInOnInitialized));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.IsValid)
            {
                ctx.RegisterOperationBlockStartAction(analyzerContext.OperationBlockStart);
            }
        });
    }

    private sealed class AnalyzerContext
    {
        public AnalyzerContext(Compilation compilation)
        {
            IJSRuntimeSymbol = compilation.GetBestTypeByMetadataName("Microsoft.JSInterop.IJSRuntime");
            JSRuntimeSymbol = compilation.GetBestTypeByMetadataName("Microsoft.JSInterop.JSRuntime");
            ProtectedBrowserStorageSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedBrowserStorage");
            WebAssemblyJSRuntimeSymbol = compilation.GetBestTypeByMetadataName("Microsoft.JSInterop.WebAssembly.WebAssemblyJSRuntime");
            WebViewJSRuntimeSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.WebView.Services.WebViewJSRuntime");
            var componentBase = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.ComponentBase");
            if (componentBase is not null)
            {
                OnInitializedMethodSymbol = componentBase.GetMembers("OnInitialized").SingleOrDefaultIfMultiple();
                OnInitializedAsyncMethodSymbol = componentBase.GetMembers("OnInitializedAsync").SingleOrDefaultIfMultiple();
            }
        }

        public INamedTypeSymbol? IJSRuntimeSymbol { get; }
        public INamedTypeSymbol? JSRuntimeSymbol { get; }
        public INamedTypeSymbol? ProtectedBrowserStorageSymbol { get; }
        public INamedTypeSymbol? WebAssemblyJSRuntimeSymbol { get; }
        public INamedTypeSymbol? WebViewJSRuntimeSymbol { get; }
        public ISymbol? OnInitializedMethodSymbol { get; }
        public ISymbol? OnInitializedAsyncMethodSymbol { get; }

        public bool IsValid
        {
            get
            {
                if (WebAssemblyJSRuntimeSymbol is not null)
                    return false; // There is no issue in WebAssembly
                
                if (WebViewJSRuntimeSymbol is not null)
                    return false; // There is no issue in WebView

                return (IJSRuntimeSymbol is not null || JSRuntimeSymbol is not null || ProtectedBrowserStorageSymbol is not null) && (OnInitializedMethodSymbol is not null || OnInitializedAsyncMethodSymbol is not null);
            }
        }

        internal void OperationBlockStart(OperationBlockStartAnalysisContext context)
        {
            if (context.OwningSymbol is not IMethodSymbol methodSymbol)
                return;

            if (methodSymbol.Override(OnInitializedMethodSymbol) || methodSymbol.Override(OnInitializedAsyncMethodSymbol))
            {
                context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
            }
        }

        private void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            var instance = operation.Instance;
            if (instance is null)
            {
                if (operation.TargetMethod.IsExtensionMethod && operation.Arguments.Length > 0)
                {
                    instance = operation.Arguments[0].Value;
                }

                if (instance is null)
                    return;
            }

            var type = instance.GetActualType();
            if (type is null)
                return;

            if (type.IsEqualTo(IJSRuntimeSymbol) || type.IsEqualTo(JSRuntimeSymbol) || type.IsOrInheritFrom(ProtectedBrowserStorageSymbol))
            {
                context.ReportDiagnostic(Rule, operation, type.Name);
            }
        }
    }
}

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class JSInvokableMethodsMustBePublicAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.JSInvokableMethodsMustBePublic,
        title: "[JSInvokable] methods must be public",
        messageFormat: "[JSInvokable] methods must be public",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.JSInvokableMethodsMustBePublic));

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
                ctx.RegisterSymbolAction(analyzerContext.AnalyzeMethod, SymbolKind.Method);
            }
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        public INamedTypeSymbol? JsInvokableSymbol { get; } = compilation.GetBestTypeByMetadataName("Microsoft.JSInterop.JSInvokableAttribute");

        public bool IsValid => JsInvokableSymbol != null;

        internal void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;
            if (method.DeclaredAccessibility != Accessibility.Public && method.HasAttribute(JsInvokableSymbol, inherits: false))
            {
                context.ReportDiagnostic(s_rule, method);
            }
        }
    }
}

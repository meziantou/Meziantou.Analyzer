namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventSourceMustBeSealedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.EventSourceMustBeSealed,
        title: "EventSource class should be sealed",
        messageFormat: "EventSource class should be sealed",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.EventSourceMustBeSealed));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (!analyzerContext.IsValid)
                return;

            ctx.RegisterSymbolAction(analyzerContext.AnalyzeNamedType, SymbolKind.NamedType);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private INamedTypeSymbol? EventSourceSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Diagnostics.Tracing.EventSource");

        /// <summary>
        /// The <c>EventSource</c> of the <c>Microsoft.Diagnostics.Tracing.EventSource</c> NuGet package.
        /// </summary>
        private INamedTypeSymbol? NuGetEventSourceSymbol { get; } = compilation.GetBestTypeByMetadataName("Microsoft.Diagnostics.Tracing.EventSource");

        public bool IsValid => EventSourceSymbol is not null || NuGetEventSourceSymbol is not null;

        public void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (symbol.TypeKind is not TypeKind.Class)
                return;

            // Abstract EventSource classes are the supported way to share code between event sources ("Utility EventSource")
            if (symbol.IsSealed || symbol.IsAbstract || symbol.IsStatic || symbol.IsImplicitlyDeclared)
                return;

            if (symbol.InheritsFrom(EventSourceSymbol) || symbol.InheritsFrom(NuGetEventSourceSymbol))
            {
                context.ReportDiagnostic(Rule, symbol);
            }
        }
    }
}

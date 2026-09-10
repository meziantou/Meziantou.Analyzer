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
            // "Microsoft.Diagnostics.Tracing.EventSource" is the type of the EventSource NuGet package
            INamedTypeSymbol?[] eventSourceSymbols =
            [
                ctx.Compilation.GetBestTypeByMetadataName("System.Diagnostics.Tracing.EventSource"),
                ctx.Compilation.GetBestTypeByMetadataName("Microsoft.Diagnostics.Tracing.EventSource"),
            ];

            if (Array.TrueForAll(eventSourceSymbols, symbol => symbol is null))
                return;

            ctx.RegisterSymbolAction(ctx =>
            {
                var symbol = (INamedTypeSymbol)ctx.Symbol;
                if (symbol.TypeKind is not TypeKind.Class)
                    return;

                // Abstract EventSource classes are the supported way to share code between event sources ("Utility EventSource")
                if (symbol.IsSealed || symbol.IsAbstract || symbol.IsStatic || symbol.IsImplicitlyDeclared)
                    return;

                if (Array.Exists(eventSourceSymbols, eventSourceSymbol => symbol.InheritsFrom(eventSourceSymbol)))
                {
                    ctx.ReportDiagnostic(Rule, symbol);
                }
            }, SymbolKind.NamedType);
        });
    }
}

using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParameterAttributeForRazorComponentAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor SupplyParameterFromQueryRule = new(
        RuleIdentifiers.SupplyParameterFromQueryRequiresParameterAttributeForRazorComponent,
        title: "Parameters with [SupplyParameterFromQuery] attributes should also be marked as [Parameter]",
        messageFormat: "Parameters with [SupplyParameterFromQuery] attributes should also be marked as [Parameter]",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SupplyParameterFromQueryRequiresParameterAttributeForRazorComponent));

    private static readonly DiagnosticDescriptor SupplyParameterFromQueryRoutableRule = new(
        RuleIdentifiers.SupplyParameterFromQueryRequiresRoutableComponent,
        title: "Parameters with [SupplyParameterFromQuery] attributes are only valid in routable components (@page)",
        messageFormat: "Parameters with [SupplyParameterFromQuery] attributes are only valid in routable components (@page)",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SupplyParameterFromQueryRequiresRoutableComponent));

    private static readonly DiagnosticDescriptor EditorRequiredRule = new(
        RuleIdentifiers.EditorRequiredRequiresParameterAttributeForRazorComponent,
        title: "Parameters with [EditorRequired] attributes should also be marked as [Parameter]",
        messageFormat: "Parameters with [EditorRequired] attributes should also be marked as [Parameter]",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.EditorRequiredRequiresParameterAttributeForRazorComponent));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(SupplyParameterFromQueryRule, EditorRequiredRule, SupplyParameterFromQueryRoutableRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.IsValid)
            {
                ctx.RegisterSymbolAction(analyzerContext.AnalyzeProperty, SymbolKind.Property);
            }
        });
    }

    private sealed class AnalyzerContext
    {
        private static readonly Version Version8 = new(8, 0);

        public AnalyzerContext(Compilation compilation)
        {
            ParameterSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.ParameterAttribute");
            SupplyParameterFromQuerySymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.SupplyParameterFromQueryAttribute");
            EditorRequiredSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.EditorRequiredAttribute");
            RouteAttributeSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.RouteAttribute");

            AspNetCoreVersion = SupplyParameterFromQuerySymbol?.ContainingAssembly.Identity.Version;
        }

        public Version? AspNetCoreVersion { get; }

        public INamedTypeSymbol? ParameterSymbol { get; }
        public INamedTypeSymbol? SupplyParameterFromQuerySymbol { get; }
        public INamedTypeSymbol? EditorRequiredSymbol { get; }
        public INamedTypeSymbol? RouteAttributeSymbol { get; }

        public bool IsValid => ParameterSymbol is not null && (SupplyParameterFromQuerySymbol is not null || EditorRequiredSymbol is not null);

        internal void AnalyzeProperty(SymbolAnalysisContext context)
        {
            // note: All attributes are sealed, no need for checking inherited types
            var property = (IPropertySymbol)context.Symbol;

            // https://devblogs.microsoft.com/dotnet/asp-net-core-updates-in-dotnet-8-preview-6/?WT.mc_id=DT-MVP-5003978#cascade-query-string-values-to-blazor-components
            if (AspNetCoreVersion < Version8)
            {
                if (property.HasAttribute(SupplyParameterFromQuerySymbol, inherits: false))
                {
                    if (!property.HasAttribute(ParameterSymbol, inherits: false))
                    {
                        context.ReportDiagnostic(SupplyParameterFromQueryRule, property);
                    }

                    if (!property.ContainingType.HasAttribute(RouteAttributeSymbol))
                    {
                        context.ReportDiagnostic(SupplyParameterFromQueryRoutableRule, property);
                    }
                }
            }

            if (property.HasAttribute(EditorRequiredSymbol, inherits: false))
            {
                if (!property.HasAttribute(ParameterSymbol, inherits: false))
                {
                    context.ReportDiagnostic(EditorRequiredRule, property);
                }
            }
        }
    }
}

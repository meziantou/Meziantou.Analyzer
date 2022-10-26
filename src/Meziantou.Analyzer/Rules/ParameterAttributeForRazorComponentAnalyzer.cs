using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParameterAttributeForRazorComponentAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_supplyParameterFromQueryRule = new(
        RuleIdentifiers.SupplyParameterFromQueryRequiresParameterAttributeForRazorComponent,
        title: "Parameters with [SupplyParameterFromQuery] attributes should also be marked as [Parameter]",
        messageFormat: "Parameters with [SupplyParameterFromQuery] attributes should also be marked as [Parameter]",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SupplyParameterFromQueryRequiresParameterAttributeForRazorComponent));

    private static readonly DiagnosticDescriptor s_supplyParameterFromQueryRoutableRule = new(
        RuleIdentifiers.SupplyParameterFromQueryRequiresRoutableComponent,
        title: "Parameters with [SupplyParameterFromQuery] attributes are only valid in routable components (@page)",
        messageFormat: "Parameters with [SupplyParameterFromQuery] attributes are only valid in routable components (@page)",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SupplyParameterFromQueryRequiresRoutableComponent));

    private static readonly DiagnosticDescriptor s_editorRequiredRule = new(
        RuleIdentifiers.EditorRequiredRequiresParameterAttributeForRazorComponent,
        title: "Parameters with [EditorRequired] attributes should also be marked as [Parameter]",
        messageFormat: "Parameters with [EditorRequired] attributes should also be marked as [Parameter]",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.EditorRequiredRequiresParameterAttributeForRazorComponent));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_supplyParameterFromQueryRule, s_editorRequiredRule, s_supplyParameterFromQueryRoutableRule);

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
        public AnalyzerContext(Compilation compilation)
        {
            ParameterSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.ParameterAttribute");
            SupplyParameterFromQuerySymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.SupplyParameterFromQueryAttribute");
            EditorRequiredSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.EditorRequiredAttribute");
            RouteAttributeSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.RouteAttribute");
        }

        public INamedTypeSymbol? ParameterSymbol { get; }
        public INamedTypeSymbol? SupplyParameterFromQuerySymbol { get; }
        public INamedTypeSymbol? EditorRequiredSymbol { get; }
        public INamedTypeSymbol? RouteAttributeSymbol { get; }

        public bool IsValid => ParameterSymbol != null && (SupplyParameterFromQuerySymbol != null || EditorRequiredSymbol != null);

        internal void AnalyzeProperty(SymbolAnalysisContext context)
        {
            var property = (IPropertySymbol)context.Symbol;

            // All attributes are sealed
            if (property.HasAttribute(SupplyParameterFromQuerySymbol, inherits: false))
            {
                if (!property.HasAttribute(ParameterSymbol, inherits: false))
                {
                    context.ReportDiagnostic(s_supplyParameterFromQueryRule, property);
                }

                if (!property.ContainingType.HasAttribute(RouteAttributeSymbol))
                {
                    context.ReportDiagnostic(s_supplyParameterFromQueryRoutableRule, property);
                }
            }

            if (property.HasAttribute(EditorRequiredSymbol, inherits: false))
            {
                if (!property.HasAttribute(ParameterSymbol, inherits: false))
                {
                    context.ReportDiagnostic(s_editorRequiredRule, property);
                }
            }
        }
    }
}

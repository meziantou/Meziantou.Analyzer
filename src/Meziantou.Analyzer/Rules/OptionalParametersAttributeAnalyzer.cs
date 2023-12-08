using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptionalParametersAttributeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor OptionalRule = new(
        RuleIdentifiers.ParametersWithDefaultValueShouldBeMarkedWithOptionalParameter,
        title: "Parameters with [DefaultParameterValue] attributes should also be marked [Optional]",
        messageFormat: "Parameters with [DefaultParameterValue] attributes should also be marked [Optional]",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ParametersWithDefaultValueShouldBeMarkedWithOptionalParameter));

    private static readonly DiagnosticDescriptor DefaultValueRule = new(
        RuleIdentifiers.DefaultValueShouldNotBeUsedWhenParameterDefaultValueIsMeant,
        title: "Use [DefaultParameterValue] instead of [DefaultValue]",
        messageFormat: "[DefaultValue] should not be used when [DefaultParameterValue] is meant",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DefaultValueShouldNotBeUsedWhenParameterDefaultValueIsMeant));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(OptionalRule, DefaultValueRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.IsValid)
            {
                ctx.RegisterSymbolAction(analyzerContext.AnalyzerParameter, SymbolKind.Parameter);
            }
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        public INamedTypeSymbol? OptionalAttributeSymbol { get; set; } = compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.OptionalAttribute");
        public INamedTypeSymbol? DefaultParameterValueAttributeSymbol { get; set; } = compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.DefaultParameterValueAttribute");
        public INamedTypeSymbol? DefaultValueAttributeSymbol { get; set; } = compilation.GetBestTypeByMetadataName("System.ComponentModel.DefaultValueAttribute");

        public bool IsValid => (OptionalAttributeSymbol is not null && DefaultParameterValueAttributeSymbol is not null) || (DefaultParameterValueAttributeSymbol is not null && DefaultValueAttributeSymbol is not null);

        public void AnalyzerParameter(SymbolAnalysisContext context)
        {
            var parameter = (IParameterSymbol)context.Symbol;
            if (parameter.HasAttribute(DefaultParameterValueAttributeSymbol) && !parameter.HasAttribute(OptionalAttributeSymbol))
            {
                context.ReportDiagnostic(OptionalRule, parameter);
            }

            if (parameter.HasAttribute(DefaultValueAttributeSymbol) && !parameter.HasAttribute(DefaultParameterValueAttributeSymbol))
            {
                context.ReportDiagnostic(DefaultValueRule, parameter);
            }
        }
    }
}

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptionalParametersAttributeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_optionalRule = new(
        RuleIdentifiers.ParametersWithDefaultValueShouldBeMarkedWithOptionalParameter,
        title: "Parameters with [DefaultParameterValue] attributes should also be marked [Optional]",
        messageFormat: "Parameters with [DefaultParameterValue] attributes should also be marked [Optional]",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ParametersWithDefaultValueShouldBeMarkedWithOptionalParameter));

    private static readonly DiagnosticDescriptor s_defaultValueRule = new(
        RuleIdentifiers.DefaultValueShouldNotBeUsedWhenParameterDefaultValueIsMeant,
        title: "Use [DefaultParameterValue] instead of [DefaultValue]",
        messageFormat: "[DefaultValue] should not be used when [DefaultParameterValue] is meant",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DefaultValueShouldNotBeUsedWhenParameterDefaultValueIsMeant));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_optionalRule, s_defaultValueRule);

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

    private sealed class AnalyzerContext
    {
        public AnalyzerContext(Compilation compilation)
        {
            OptionalAttributeSymbol = compilation.GetTypeByMetadataName("System.Runtime.InteropServices.OptionalAttribute");
            DefaultParameterValueAttributeSymbol = compilation.GetTypeByMetadataName("System.Runtime.InteropServices.DefaultParameterValueAttribute");
            DefaultValueAttributeSymbol = compilation.GetTypeByMetadataName("System.ComponentModel.DefaultValueAttribute");
        }

        public INamedTypeSymbol? OptionalAttributeSymbol { get; set; }
        public INamedTypeSymbol? DefaultParameterValueAttributeSymbol { get; set; }
        public INamedTypeSymbol? DefaultValueAttributeSymbol { get; set; }

        public bool IsValid => (OptionalAttributeSymbol != null && DefaultParameterValueAttributeSymbol != null) || (DefaultParameterValueAttributeSymbol != null && DefaultValueAttributeSymbol != null);

        public void AnalyzerParameter(SymbolAnalysisContext context)
        {
            var parameter = (IParameterSymbol)context.Symbol;
            if (parameter.HasAttribute(DefaultParameterValueAttributeSymbol) && !parameter.HasAttribute(OptionalAttributeSymbol))
            {
                context.ReportDiagnostic(s_optionalRule, parameter);
            }

            if (parameter.HasAttribute(DefaultValueAttributeSymbol) && !parameter.HasAttribute(DefaultParameterValueAttributeSymbol))
            {
                context.ReportDiagnostic(s_defaultValueRule, parameter);
            }
        }
    }
}

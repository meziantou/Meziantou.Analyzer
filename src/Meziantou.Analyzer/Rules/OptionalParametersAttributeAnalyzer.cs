using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class OptionalParametersAttributeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.ParametersWithDefaultValueShouldBeMarkedWithOptionalParameter,
            title: "Parameters with [DefaultParameterValue] attributes should also be marked [Optional]",
            messageFormat: "Parameters with [DefaultParameterValue] attributes should also be marked [Optional]",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ParametersWithDefaultValueShouldBeMarkedWithOptionalParameter));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

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
            }

            public INamedTypeSymbol? OptionalAttributeSymbol { get; set; }
            public INamedTypeSymbol? DefaultParameterValueAttributeSymbol { get; set; }

            public bool IsValid => OptionalAttributeSymbol != null && DefaultParameterValueAttributeSymbol != null;

            public void AnalyzerParameter(SymbolAnalysisContext context)
            {
                var parameter = (IParameterSymbol)context.Symbol;
                if (parameter.HasAttribute(DefaultParameterValueAttributeSymbol) && !parameter.HasAttribute(OptionalAttributeSymbol))
                {
                    context.ReportDiagnostic(s_rule, parameter);
                }
            }
        }
    }
}

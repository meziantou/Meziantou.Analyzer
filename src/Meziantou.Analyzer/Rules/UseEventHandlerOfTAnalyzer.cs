using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UseEventHandlerOfTAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.UseEventHandlerOfT,
            title: "Use EventHandler<T>",
            messageFormat: "Use EventHandler<T>",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseEventHandlerOfT));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {
                var analyzerContext = new AnalyzerContext(ctx.Compilation);
                ctx.RegisterSymbolAction(analyzerContext.AnalyzeSymbol, SymbolKind.Event);
            });
        }

        private sealed class AnalyzerContext
        {
            public AnalyzerContext(Compilation compilation)
            {
                EventHandlerSymbol = compilation.GetTypeByMetadataName("System.EventHandler");
                EventHandlerOfTSymbol = compilation.GetTypeByMetadataName("System.EventHandler`1");
                RoutedPropertyChangedEventHandlerOfTSymbol = compilation.GetTypeByMetadataName("System.Windows.RoutedPropertyChangedEventHandler`1");
            }

            public INamedTypeSymbol EventHandlerSymbol { get; }
            public INamedTypeSymbol EventHandlerOfTSymbol { get; }
            public INamedTypeSymbol RoutedPropertyChangedEventHandlerOfTSymbol { get; }

            public void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                var symbol = (IEventSymbol)context.Symbol;
                if (symbol.Type.OriginalDefinition.IsEqualTo(EventHandlerOfTSymbol) ||
                    symbol.Type.OriginalDefinition.IsEqualTo(EventHandlerSymbol) ||
                    symbol.Type.OriginalDefinition.IsEqualTo(RoutedPropertyChangedEventHandlerOfTSymbol))
                {
                    return;
                }

                if (symbol.IsInterfaceImplementation())
                    return;

                context.ReportDiagnostic(s_rule, symbol);
            }
        }
    }
}

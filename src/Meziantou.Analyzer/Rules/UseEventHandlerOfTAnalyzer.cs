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
                EventArgsSymbol = compilation.GetTypeByMetadataName("System.EventArgs");
            }

            public INamedTypeSymbol EventArgsSymbol { get; }

            public void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                var symbol = (IEventSymbol)context.Symbol;
                var method = (symbol.Type as INamedTypeSymbol)?.DelegateInvokeMethod;
                if (method == null)
                    return;

                if (IsValidSignature(method))
                    return;

                if (symbol.IsInterfaceImplementation())
                    return;

                context.ReportDiagnostic(s_rule, symbol);
            }

            private bool IsValidSignature(IMethodSymbol methodSymbol)
            {
                return methodSymbol.ReturnsVoid
                    && methodSymbol.Arity == 0
                    && methodSymbol.Parameters.Length == 2
                    && methodSymbol.Parameters[0].Type.IsObject()
                    && methodSymbol.Parameters[1].Type.IsOrInheritFrom(EventArgsSymbol);
            }
        }
    }
}

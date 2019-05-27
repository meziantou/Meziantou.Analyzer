using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EventArgsNameShouldEndWithEventArgsAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.EventArgsNameShouldEndWithEventArgs,
            title: "Class name should end with 'EventArgs'",
            messageFormat: "Class name should end with 'EventArgs'",
            RuleCategories.Naming,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.EventArgsNameShouldEndWithEventArgs));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (symbol.Name == null)
                return;

            if (!symbol.Name.EndsWith("EventArgs", System.StringComparison.Ordinal) && symbol.InheritsFrom(context.Compilation.GetTypeByMetadataName("System.EventArgs")))
            {
                context.ReportDiagnostic(s_rule, symbol);
            }
        }
    }
}

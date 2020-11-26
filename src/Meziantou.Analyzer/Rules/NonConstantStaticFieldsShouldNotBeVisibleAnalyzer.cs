using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NonConstantStaticFieldsShouldNotBeVisibleAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
            RuleIdentifiers.NonConstantStaticFieldsShouldNotBeVisible,
            title: "Non-constant static fields should not be visible",
            messageFormat: "Non-constant static fields should not be visible",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.NonConstantStaticFieldsShouldNotBeVisible));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Field);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var symbol = (IFieldSymbol)context.Symbol;
            if (!symbol.IsStatic || symbol.IsReadOnly || symbol.IsConst || !symbol.IsVisibleOutsideOfAssembly())
                return;

            // Skip enumerations
            if (symbol.ContainingSymbol is INamedTypeSymbol typeSymbol && typeSymbol.EnumUnderlyingType != null)
                return;

            context.ReportDiagnostic(s_rule, symbol);
        }
    }
}

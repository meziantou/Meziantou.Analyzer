using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MarkAttributesWithAttributeUsageAttribute : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.MarkAttributesWithAttributeUsageAttribute,
            title: "Mark attributes with AttributeUsageAttribute",
            messageFormat: "Mark attributes with AttributeUsageAttribute",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MarkAttributesWithAttributeUsageAttribute));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
        }

        private void Analyze(SymbolAnalysisContext context)
        {
            var attributeType = context.Compilation.GetTypeByMetadataName("System.Attribute");
            var attributeUsageAttributeType = context.Compilation.GetTypeByMetadataName("System.AttributeUsageAttribute");
            if (attributeType == null || attributeUsageAttributeType == null)
                return;

            var symbol = (INamedTypeSymbol)context.Symbol;
            if (!symbol.InheritsFrom(attributeType) || symbol.HasAttribute(attributeUsageAttributeType))
                return;

            context.ReportDiagnostic(s_rule, symbol);
        }
    }
}

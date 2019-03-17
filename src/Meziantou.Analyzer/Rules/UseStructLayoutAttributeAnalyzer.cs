using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseStructLayoutAttributeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.MissingStructLayoutAttribute,
            title: "Add StructLayoutAttribute",
            messageFormat: "Add StructLayoutAttribute",
            RuleCategories.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MissingStructLayoutAttribute));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var attributeType = compilationContext.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
                if (attributeType == null)
                    return;

                compilationContext.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
            });
        }

        private void Analyze(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (!symbol.IsValueType)
                return;

            var attributeType = context.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
            if (attributeType == null)
                return;

            if (symbol.GetAttributes().Any(attr => attributeType.Equals(attr.AttributeClass)))
                return;

            context.ReportDiagnostic(s_rule, symbol);
        }
    }
}

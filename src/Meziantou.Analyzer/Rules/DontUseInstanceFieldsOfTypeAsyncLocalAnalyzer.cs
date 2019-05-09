using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DontUseInstanceFieldsOfTypeAsyncLocalAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.DontUseInstanceFieldsOfTypeAsyncLocal,
            title: "Don't use instance fields of type AsyncLocal<T>",
            messageFormat: "Don't use instance fields of type AsyncLocal<T>",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DontUseInstanceFieldsOfTypeAsyncLocal));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(Analyze, SymbolKind.Field);
        }

        private static void Analyze(SymbolAnalysisContext context)
        {
            var field = (IFieldSymbol)context.Symbol;
            if (field.IsStatic)
                return;

            var type = context.Compilation.GetTypeByMetadataName("System.Threading.AsyncLocal`1");
            if (field.Type.OriginalDefinition.IsEqualTo(type))
            {
                context.ReportDiagnostic(s_rule, field);
            }
        }
    }
}

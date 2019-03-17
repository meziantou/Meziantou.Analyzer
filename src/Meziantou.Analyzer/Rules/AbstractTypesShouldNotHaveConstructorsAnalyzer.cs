using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AbstractTypesShouldNotHaveConstructorsAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.AbstractTypesShouldNotHaveConstructors,
            title: "Abstract types should not have public or internal constructors",
            messageFormat: "Abstract types should not have public or internal constructors",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AbstractTypesShouldNotHaveConstructors));

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
            if (!symbol.IsAbstract)
                return;

            foreach (var ctor in symbol.InstanceConstructors)
            {
                if (ctor.DeclaredAccessibility == Accessibility.Public || ctor.DeclaredAccessibility == Accessibility.Internal)
                {
                    var syntax = ctor.DeclaringSyntaxReferences.FirstOrDefault();
                    if (syntax != null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(s_rule, Location.Create(syntax.SyntaxTree, syntax.Span)));
                    }
                }
            }
        }
    }
}

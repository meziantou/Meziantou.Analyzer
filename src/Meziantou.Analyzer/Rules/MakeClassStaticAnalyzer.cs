using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MakeClassStaticAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.MakeClassStatic,
            title: "Make class static",
            messageFormat: "Make class static",
            RuleCategories.Design,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AbstractTypesShouldNotHaveConstructors));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(s =>
            {
                var potentialClasses = new List<ITypeSymbol>();
                var parentClasses = new HashSet<ITypeSymbol>();

                s.RegisterSymbolAction(ctx =>
                {
                    var symbol = (INamedTypeSymbol)ctx.Symbol;
                    if (!symbol.IsReferenceType)
                        return;

                    if (IsPotentialStatic(symbol))
                    {
                        potentialClasses.Add(symbol);
                    }

                    if (symbol.BaseType != null)
                    {
                        parentClasses.Add(symbol.BaseType);
                    }
                }, SymbolKind.NamedType);

                s.RegisterCompilationEndAction(ctx =>
                {
                    foreach (var c in potentialClasses)
                    {
                        if (parentClasses.Contains(c))
                            continue;

                        foreach (var location in c.Locations)
                        {
                            ctx.ReportDiagnostic(Diagnostic.Create(s_rule, location));
                        }
                    }
                });
            });
        }

        private bool IsPotentialStatic(INamedTypeSymbol symbol)
        {
            return !symbol.IsAbstract &&
                !symbol.IsStatic &&
                !symbol.Interfaces.Any() &&
                (symbol.BaseType == null || symbol.BaseType.SpecialType == SpecialType.System_Object) &&
                symbol.GetMembers().All(member => member.IsStatic || member.IsImplicitlyDeclared);
        }
    }
}

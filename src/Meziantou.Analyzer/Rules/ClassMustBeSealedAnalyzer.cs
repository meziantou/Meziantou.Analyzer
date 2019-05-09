using System.Collections.Generic;
using System.Collections.Immutable;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ClassMustBeSealedAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.ClassMustBeSealed,
            title: "Make class sealed",
            messageFormat: "Make class sealed",
            RuleCategories.Design,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ClassMustBeSealed));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterCompilationStartAction(ctx =>
            {
                var analyzerContext = new AnalyzerContext();

                ctx.RegisterSymbolAction(analyzerContext.AnalyzeNamedTypeSymbol, SymbolKind.NamedType);
                ctx.RegisterCompilationEndAction(analyzerContext.AnalyzeCompilationEnd);
            });
        }

        private static bool IsPotentialSealed(AnalyzerOptions options, INamedTypeSymbol symbol)
        {
            if (symbol.IsSealed || symbol.IsAbstract || symbol.IsStatic || symbol.IsValueType)
                return false;

            if (symbol.IsVisibleOutsideOfAssembly() && PublicClassShouldBeSealed(options, symbol))
                return false;

            return true;
        }

        private static bool PublicClassShouldBeSealed(AnalyzerOptions options, ISymbol symbol)
        {
            foreach (var location in symbol.Locations)
            {
                if (options.GetConfigurationValue(location.SourceTree.FilePath, RuleIdentifiers.ClassMustBeSealed + ".public_class_should_be_sealed", (bool?)null) == true)
                    return true;
            }

            return false;
        }

        private sealed class AnalyzerContext
        {
            private readonly List<ITypeSymbol> _potentialClasses = new List<ITypeSymbol>();
            private readonly HashSet<ITypeSymbol> _cannotBeSealedClasses = new HashSet<ITypeSymbol>();

            public void AnalyzeNamedTypeSymbol(SymbolAnalysisContext context)
            {
                var symbol = (INamedTypeSymbol)context.Symbol;
                switch (symbol.TypeKind)
                {
                    case TypeKind.Class:
                        if (IsPotentialSealed(context.Options, symbol))
                        {
                            lock (_potentialClasses)
                            {
                                _potentialClasses.Add(symbol);
                            }
                        }

                        if (symbol.BaseType != null)
                        {
                            lock (_cannotBeSealedClasses)
                            {
                                _cannotBeSealedClasses.Add(symbol.BaseType);
                            }
                        }

                        break;
                }
            }

            public void AnalyzeCompilationEnd(CompilationAnalysisContext context)
            {
                foreach (var @class in _potentialClasses)
                {
                    if (_cannotBeSealedClasses.Contains(@class))
                        continue;

                    context.ReportDiagnostic(s_rule, @class);
                }
            }
        }
    }
}

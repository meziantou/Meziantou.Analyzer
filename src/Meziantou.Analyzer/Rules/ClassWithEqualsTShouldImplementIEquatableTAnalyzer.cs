using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ClassWithEqualsTShouldImplementIEquatableTAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.ClassWithEqualsTShouldImplementIEquatableT,
            title: "A class that provides Equals(T) should implement IEquatable<T>",
            messageFormat: "A class that provides Equals(T) should implement IEquatable<T>",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ClassWithEqualsTShouldImplementIEquatableT));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeMethodSymbol, SymbolKind.Method);
        }

        private static void AnalyzeMethodSymbol(SymbolAnalysisContext context)
        {
            var equalsMethod = AsEqualsMethod(context.Symbol);
            if (equalsMethod is null)
                return;

            var genericInterfaceSymbol = context.Compilation.GetTypeByMetadataName("System.IEquatable`1");
            if (genericInterfaceSymbol == null)
                return;

            var concreteInterfaceSymbol = genericInterfaceSymbol.Construct(equalsMethod.ContainingType);
            if (equalsMethod.ContainingType.Implements(concreteInterfaceSymbol))
                return;

            context.ReportDiagnostic(s_rule, equalsMethod.ContainingType);
        }

        /// <summary>
        /// Determines if an <see cref="ISymbol"/> has the same signature as <see cref="IEquatable{T}.Equals(T)"/>,
        /// in which case it's returned as an <see cref="IMethodSymbol"/>. Else, null is returned.
        /// </summary>
        /// <param name="symbol">Symbol to be assessed</param>
        /// <returns>The input ISymbol cast to IMethodSymbol, if it has the right signature; null otherwise</returns>
        internal static IMethodSymbol AsEqualsMethod(ISymbol symbol)
        {
            if (symbol.Kind != SymbolKind.Method)
                return null;

            if (!symbol.Name.Equals("Equals", StringComparison.Ordinal))
                return null;

            var equalsMethod = (IMethodSymbol)symbol;
            if (equalsMethod.IsStatic ||
                equalsMethod.IsAbstract ||
                equalsMethod.MethodKind != MethodKind.Ordinary ||
                equalsMethod.DeclaredAccessibility != Accessibility.Public)
            {
                return null;
            }

            if (equalsMethod.Parameters.Length != 1 ||
                !equalsMethod.Parameters[0].Type.IsEqualTo(equalsMethod.ContainingType) ||
                !equalsMethod.ReturnType.IsBoolean())
            {
                return null;
            }

            return equalsMethod;
        }
    }
}

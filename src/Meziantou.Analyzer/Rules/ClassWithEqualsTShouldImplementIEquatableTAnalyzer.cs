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
            var equalsMethod = (IMethodSymbol)context.Symbol;

            if (!equalsMethod.Name.Equals("Equals", StringComparison.Ordinal))
                return;

            if (equalsMethod.IsStatic ||
                equalsMethod.IsAbstract ||
                equalsMethod.MethodKind != MethodKind.Ordinary ||
                equalsMethod.DeclaredAccessibility != Accessibility.Public)
            {
                return;
            }

            if (equalsMethod.Parameters.Length != 1 ||
                !equalsMethod.Parameters[0].Type.IsEqualTo(equalsMethod.ContainingType) ||
                !equalsMethod.ReturnType.IsBoolean())
            {
                return;
            }

            var genericInterfaceSymbol = context.Compilation.GetTypeByMetadataName("System.IEquatable`1");
            if (genericInterfaceSymbol == null)
                return;

            var concreteInterfaceSymbol = genericInterfaceSymbol.Construct(equalsMethod.ContainingType);
            if (equalsMethod.ContainingType.Implements(concreteInterfaceSymbol))
                return;

            context.ReportDiagnostic(s_rule, equalsMethod.ContainingType);
        }
    }
}

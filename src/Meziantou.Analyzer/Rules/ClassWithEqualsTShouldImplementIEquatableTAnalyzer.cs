using System;
using System.Collections.Immutable;
using System.Linq;
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
            var symbol = (IMethodSymbol)context.Symbol;

            if (!string.Equals(symbol.Name, "Equals", StringComparison.Ordinal))
                return;

            if (symbol.IsStatic || symbol.MethodKind != MethodKind.Ordinary || symbol.DeclaredAccessibility != Accessibility.Public)
                return;

            if (symbol.Parameters.Length != 1 || !symbol.Parameters[0].Type.IsEqualTo(symbol.ContainingType) || !symbol.ReturnType.IsBoolean())
                return;

            if (symbol.ContainingType.Interfaces.Any(i => i.Name.Equals("IEquatable", StringComparison.Ordinal)))
                return;

            context.ReportDiagnostic(s_rule, symbol.ContainingType);
        }
    }
}

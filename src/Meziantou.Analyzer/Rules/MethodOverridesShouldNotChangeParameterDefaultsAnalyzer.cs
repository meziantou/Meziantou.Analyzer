﻿using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MethodOverridesShouldNotChangeParameterDefaultsAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.MethodOverridesShouldNotChangeParameterDefaults,
            title: "Method overrides should not change parameter defaults",
            messageFormat: "Method overrides should not change parameter defaults ({0})",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MethodOverridesShouldNotChangeParameterDefaults));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;
            if (method.IsImplicitlyDeclared || method.Parameters.Length == 0)
                return;

            IMethodSymbol baseSymbol = null;
            if (method.IsOverride)
            {
                baseSymbol = method.OverriddenMethod;
            }
            else
            {
                baseSymbol = method.GetImplementingInterfaceSymbol();
            }

            if (baseSymbol == null)
                return;

            foreach (var parameter in method.Parameters)
            {
                if (parameter.IsImplicitlyDeclared || !parameter.HasExplicitDefaultValue || parameter.IsThis)
                    continue;

                var originalParameter = baseSymbol.Parameters[parameter.Ordinal];
                if (!Equals(originalParameter.ExplicitDefaultValue, parameter.ExplicitDefaultValue))
                {
                    context.ReportDiagnostic(s_rule, parameter, FormattableString.Invariant($"{originalParameter.ExplicitDefaultValue}"));
                }
            }
        }
    }
}

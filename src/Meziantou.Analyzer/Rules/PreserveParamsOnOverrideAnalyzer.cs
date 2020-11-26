using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PreserveParamsOnOverrideAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
            RuleIdentifiers.PreserveParamsOnOverride,
            title: "Method overrides should not omit params keyword",
            messageFormat: "Method overrides should not omit params keyword",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.PreserveParamsOnOverride));

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

            if (method.ExplicitInterfaceImplementations.Length > 0)
                return;

            IMethodSymbol? baseSymbol;
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
                if (parameter.IsImplicitlyDeclared || parameter.IsThis)
                    continue;

                var originalParameter = baseSymbol.Parameters[parameter.Ordinal];

                // We cannot use parameter.IsParams because on overrided member this is true as it is implicitly inherited.
                // Instead we need to check if the syntax contains the keyword explicitly
                if (originalParameter.IsParams)
                {
                    if (HasParamsKeyword(parameter, context.CancellationToken))
                        continue;

                    context.ReportDiagnostic(s_rule, parameter);
                }
            }

            static bool HasParamsKeyword(IParameterSymbol parameter, CancellationToken cancellationToken)
            {
                foreach (var syntaxReference in parameter.DeclaringSyntaxReferences)
                {
                    var syntax = syntaxReference.GetSyntax(cancellationToken) as ParameterSyntax;
                    if (syntax == null)
                        continue;

                    if (syntax.Modifiers.Any(modifier => modifier.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ParamsKeyword)))
                        return true;
                }

                return false;
            }
        }
    }
}

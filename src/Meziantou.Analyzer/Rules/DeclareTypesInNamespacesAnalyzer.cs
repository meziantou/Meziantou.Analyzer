using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DeclareTypesInNamespacesAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
            RuleIdentifiers.DeclareTypesInNamespaces,
            title: "Declare types in namespaces",
            messageFormat: "Declare type '{0}' in a namespace",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DeclareTypesInNamespaces));

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
            if (symbol.IsImplicitlyDeclared || symbol.IsImplicitClass || symbol.Name.Contains('$', System.StringComparison.Ordinal))
                return;

            if (IsTopLevelStatement(symbol, context.CancellationToken))
                return;

            if (symbol.ContainingType == null && (symbol.ContainingNamespace?.IsGlobalNamespace ?? true))
            {
                context.ReportDiagnostic(s_rule, symbol, symbol.Name);
            }
        }

        private static bool IsTopLevelStatement(ISymbol symbol, CancellationToken cancellationToken)
        {
            if (symbol.DeclaringSyntaxReferences.Length == 0)
                return false;

            foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
            {
                var syntax = syntaxReference.GetSyntax(cancellationToken);
                if (!syntax.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.CompilationUnit))
                    return false;
            }

            return true;
        }
    }
}

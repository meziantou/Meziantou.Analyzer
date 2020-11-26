using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotThrowFromFinallyBlockAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
            RuleIdentifiers.DoNotThrowFromFinallyBlock,
            title: "Do not throw from a finally block",
            messageFormat: "Do not throw from a finally block",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotThrowFromFinallyBlock));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeFinallyClause, SyntaxKind.FinallyClause);
        }

        private static void AnalyzeFinallyClause(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not FinallyClauseSyntax finallyClause)
                return;

            var finallyBlock = finallyClause.Block;
            if (finallyBlock is null)
                return;

            foreach (var throwStatement in finallyBlock.DescendantNodes().Where(IsThrowStatement))
            {
                context.ReportDiagnostic(s_rule, throwStatement);
            }
        }

        private static bool IsThrowStatement(SyntaxNode node) => node.IsKind(SyntaxKind.ThrowStatement);
    }
}

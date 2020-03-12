using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AvoidUsingRedundantElseAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.AvoidUsingRedundantElse,
            title: "Avoid using redundant 'else'",
            messageFormat: "Avoid using redundant 'else'",
            RuleCategories.Style,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "The 'if' block contains a jump statement (break, continue, goto, return, throw, yield break). Using 'else' is redundant and needlessly maintains a higher nesting level.",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidUsingRedundantElse));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeElseClause, SyntaxKind.ElseClause);
        }

        private static void AnalyzeElseClause(SyntaxNodeAnalysisContext context)
        {
            var elseClauseSyntax = (ElseClauseSyntax)context.Node;
            if (elseClauseSyntax is null)
                return;

            if (!(elseClauseSyntax.Parent is IfStatementSyntax ifStatementSyntax))
                return;

            var statementOrBlock = ifStatementSyntax.Statement;
            if (statementOrBlock is null)
                return;

            var result = context.SemanticModel.AnalyzeControlFlow(statementOrBlock);
            if (!result.Succeeded)
                return;

            if (!result.EndPointIsReachable || result.ExitPoints.Any(ep => IsDirectAccess(ifStatementSyntax, ep)))
            {
                context.ReportDiagnostic(s_rule, elseClauseSyntax.ElseKeyword);
            }
        }

        /// <summary>
        /// Determines if a given 'if' statement's access to an exit point is straightforward.
        /// For instance, access to an exit point in a nested 'if' would not be considered straightforward.
        /// </summary>
        /// <param name="ifStatementSyntax">The 'if' statement whose 'else' is currently under scrutiny</param>
        /// <param name="exitPoint">A reachable exit point detected by the semantic model</param>
        /// <returns>true if the exit point is directly accessible, false otherwise</returns>
        private static bool IsDirectAccess(IfStatementSyntax ifStatementSyntax, SyntaxNode exitPoint)
        {
            var node = exitPoint.Parent;
            while (node != null)
            {
                if (node == ifStatementSyntax)
                    return true;
                if (!(node is BlockSyntax))
                    break;
                node = node.Parent;
            }
            return false;
        }
    }
}

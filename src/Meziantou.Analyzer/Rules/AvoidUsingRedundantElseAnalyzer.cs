using System.Collections.Generic;
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
            title: "Avoid using redundant else",
            messageFormat: "Avoid using redundant else",
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
            var elseClause = (ElseClauseSyntax)context.Node;
            if (elseClause is null)
                return;

            if (!(elseClause.Parent is IfStatementSyntax ifStatement))
                return;

            var thenStatement = ifStatement.Statement;
            var elseStatement = elseClause.Statement;
            if (thenStatement is null || elseStatement is null)
                return;

            // If the 'else' clause contains a "using statement local declaration" as direct child, return
            // NOTE:
            //  using var charEnumerator = "".GetEnumerator();          => LocalDeclarationStatementSyntax  (will return)
            //  using (var charEnumerator = "".GetEnumerator()) { }     => UsingStatementSyntax             (will carry on)
            var elseHasUsingStatementLocalDeclaration = GetElseClauseChildren(elseClause)
                .OfType<LocalDeclarationStatementSyntax>()
                .Any(localDeclaration => localDeclaration.UsingKeyword.IsKind(SyntaxKind.UsingKeyword));
            if (elseHasUsingStatementLocalDeclaration)
                return;

            // If there are conflicting local (variable or function) declarations in 'if' and 'else' blocks, return
            var thenLocalIdentifiers = FindLocalIdentifiersIn(thenStatement);
            var elseLocalIdentifiers = FindLocalIdentifiersIn(elseStatement);
            if (thenLocalIdentifiers.Intersect(elseLocalIdentifiers).Any())
                return;

            var controlFlowAnalysis = context.SemanticModel.AnalyzeControlFlow(thenStatement);
            if (controlFlowAnalysis == null || !controlFlowAnalysis.Succeeded)
                return;

            if (!controlFlowAnalysis.EndPointIsReachable || controlFlowAnalysis.ExitPoints.Any(ep => IsDirectAccess(ifStatement, ep)))
            {
                context.ReportDiagnostic(s_rule, elseClause.ElseKeyword);
            }
        }

        internal static IEnumerable<SyntaxNode> GetElseClauseChildren(ElseClauseSyntax elseClauseSyntax)
        {
            return elseClauseSyntax.Statement is BlockSyntax elseBlockSyntax ?
                elseBlockSyntax.ChildNodes() :
                new[] { elseClauseSyntax.Statement };
        }

        private static IEnumerable<string> FindLocalIdentifiersIn(SyntaxNode node)
        {
            foreach(var child in node.DescendantNodes())
            {
                switch (child)
                {
                    case VariableDeclaratorSyntax variableDeclarator:
                        yield return variableDeclarator.Identifier.Text;
                        break;

                    case LocalFunctionStatementSyntax localFunction:
                        yield return localFunction.Identifier.Text;
                        break;

                    case SingleVariableDesignationSyntax singleVariableDesignation:
                        yield return singleVariableDesignation.Identifier.Text;
                        break;
                }
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

                if (!node.IsKind(SyntaxKind.Block))
                    break;

                node = node.Parent;
            }

            return false;
        }
    }
}

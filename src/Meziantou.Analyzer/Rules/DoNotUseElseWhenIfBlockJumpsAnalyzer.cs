using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotUseElseWhenIfBlockJumpsAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotUseElseWhenIfBlockJumps,
            title: "Do not use 'else' when 'if' block jumps",
            messageFormat: "Do not use 'else' when 'if' block jumps",
            RuleCategories.Style,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "The 'if' block ends with a jump statement (break, continue, goto, return, throw, yield). Using 'else' is redundant and needlessly maintains a higher nesting level.",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseElseWhenIfBlockJumps));

        private static readonly Type[] s_jumpStatementSyntaxTypes =
        {
            typeof(BreakStatementSyntax),
            typeof(ContinueStatementSyntax),
            typeof(GotoStatementSyntax),
            typeof(ReturnStatementSyntax),
            typeof(ThrowStatementSyntax),
            typeof(YieldStatementSyntax),
        };

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

            var ifStatementSyntax = elseClauseSyntax.Parent;
            var ifStatementNodes = ifStatementSyntax.ChildNodes();

            // The 1st node is the 'if' statement's condition,
            // the 2nd one, a StatementSyntax or a BlockSyntax (i.e. the node we are interested in)
            // the last one, the ElseClauseSyntax.
            var statementOrBlock = ifStatementNodes.Skip(1).Take(1).First();
            if (IsOrEndsWithJumpStatement(statementOrBlock))
            {
                context.ReportDiagnostic(s_rule, elseClauseSyntax);
            }
        }

        private static bool IsOrEndsWithJumpStatement(SyntaxNode node)
        {
            if (s_jumpStatementSyntaxTypes.Any(t => t == node.GetType()))
                return true;

            if (node is BlockSyntax && s_jumpStatementSyntaxTypes.Any(t => t == node.ChildNodes().Last().GetType()))
                return true;

            return false;
        }
    }
}

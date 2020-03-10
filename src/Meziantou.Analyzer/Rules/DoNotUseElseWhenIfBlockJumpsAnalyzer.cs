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
            description: "The 'if' block contains a jump statement (break, continue, goto, return, throw, yield). Using 'else' is redundant and needlessly maintains a higher nesting level.",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseElseWhenIfBlockJumps));

        private static readonly Type[] s_jumpStatementTypes =
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
            if (elseClauseSyntax is null)
                return;

            if (!(elseClauseSyntax.Parent is IfStatementSyntax ifStatementSyntax))
                return;

            var statementOrBlock = ifStatementSyntax.Statement;
            if (statementOrBlock is null)
                return;

            if (ContainsJumpStatement(statementOrBlock))
            {
                context.ReportDiagnostic(s_rule, elseClauseSyntax);
            }
        }

        private static bool ContainsJumpStatement(SyntaxNode node)
        {
            if (node is BlockSyntax blockSyntax)
                return blockSyntax.Statements.Any(IsJumpStatement);
            return (IsJumpStatement(node));

            bool IsJumpStatement(SyntaxNode node) => s_jumpStatementTypes.Any(jumpStatementType => jumpStatementType == node.GetType());
        }
    }
}

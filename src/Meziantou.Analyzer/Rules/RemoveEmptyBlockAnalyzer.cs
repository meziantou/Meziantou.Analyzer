using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RemoveEmptyBlockAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.RemoveEmptyBlock,
        title: "Remove empty else/finally block",
        messageFormat: "Remove empty {0} block",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.RemoveEmptyBlock));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeFinally, SyntaxKind.FinallyClause);
        context.RegisterSyntaxNodeAction(AnalyzeElse, SyntaxKind.ElseClause);
    }

    private static void AnalyzeFinally(SyntaxNodeAnalysisContext context)
    {
        var finallyClause = (FinallyClauseSyntax)context.Node;
        if (IsEmptyBlock(finallyClause.Block))
        {
            context.ReportDiagnostic(s_rule, finallyClause, "finally");
        }
    }

    private static void AnalyzeElse(SyntaxNodeAnalysisContext context)
    {
        var elseClause = (ElseClauseSyntax)context.Node;
        if (elseClause.Statement is BlockSyntax blockSyntax)
        {
            if (IsEmptyBlock(blockSyntax))
            {
                context.ReportDiagnostic(s_rule, elseClause, "else");
            }
        }
    }

    private static bool IsEmptyBlock(BlockSyntax? block)
    {
        if (block != null)
        {
            if (block.Statements.Count > 0)
                return false;

            if (block.ContainsDirectives)
                return false;

            if (block.DescendantTrivia().Any(node => node.IsKind(SyntaxKind.MultiLineCommentTrivia) || node.IsKind(SyntaxKind.SingleLineCommentTrivia)))
                return false;
        }

        return true;
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

internal static class AvoidUsingRedundantElseAnalyzerCommon
{
    internal static IEnumerable<SyntaxNode> GetElseClauseChildren(ElseClauseSyntax elseClauseSyntax)
    {
        return elseClauseSyntax.Statement is BlockSyntax elseBlockSyntax ?
            elseBlockSyntax.ChildNodes() :
            [elseClauseSyntax.Statement];
    }
}

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

internal static class AvoidUsingRedundantElseAnalyzerCommon
{
    internal static IEnumerable<SyntaxNode> GetElseClauseChildren(ElseClauseSyntax elseClauseSyntax)
    {
        return elseClauseSyntax.Statement is BlockSyntax elseBlockSyntax ?
            elseBlockSyntax.ChildNodes() :
            new[] { elseClauseSyntax.Statement };
    }
}

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Internals;

internal static class SyntaxNodeExtensions
{
    /// <summary>
    /// Enumerates the descendant nodes of <paramref name="node"/> that are part of its execution flow.
    /// The bodies of the nested lambdas, anonymous methods and local functions are not enumerated as
    /// they are only declared, not executed, at that location.
    /// </summary>
    public static IEnumerable<SyntaxNode> DescendantNodesInSameExecutionFlow(this SyntaxNode node)
        => node.DescendantNodes(descendIntoChildren: child => child == node || child is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax));
}

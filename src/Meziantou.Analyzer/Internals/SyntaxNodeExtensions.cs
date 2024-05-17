using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer.Internals;
internal static class SyntaxNodeExtensions
{
    public static T WithoutTrailingSpacesTrivia<T>(this T syntaxNode) where T : SyntaxNode
    {
        if (!syntaxNode.HasTrailingTrivia)
            return syntaxNode;

        return syntaxNode.WithTrailingTrivia(
            syntaxNode.GetTrailingTrivia().Reverse().SkipWhile(t => t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.WhitespaceTrivia)).Reverse());
    }
}

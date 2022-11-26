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

        var trivia = syntaxNode.GetTrailingTrivia().Reverse();
        var newTrivia = trivia.SkipWhile(t => t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.WhitespaceTrivia));
        return syntaxNode.WithTrailingTrivia(newTrivia.Reverse());
    }
}

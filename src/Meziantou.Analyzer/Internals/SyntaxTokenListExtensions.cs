using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Meziantou.Analyzer;

internal static class SyntaxTokenListExtensions
{
    private static readonly string[] ModifiersSortOrder = GetModifiersOrder();

    private static string[] GetModifiersOrder()
    {
        return ["public", "private", "protected", "internal", "const", "static", "extern", "new", "virtual", "abstract", "sealed", "override", "readonly", "unsafe", "volatile", "async"];
    }

    public static SyntaxTokenList Remove(this SyntaxTokenList list, SyntaxKind syntaxToRemove)
    {
        var existingSyntax = list.FirstOrDefault(token => token.IsKind(syntaxToRemove));
        if (existingSyntax != default)
        {
            return list.Remove(existingSyntax);
        }

        return list;
    }

    public static SyntaxTokenList Add(this SyntaxTokenList list, SyntaxKind syntaxKind)
    {
        return Add(list, SyntaxFactory.Token(syntaxKind));
    }

    public static SyntaxTokenList Add(this SyntaxTokenList list, SyntaxToken syntaxToken)
    {
        var newSyntaxOrder = IndexOf(syntaxToken);
        if (newSyntaxOrder >= 0)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var index = IndexOf(list[i]);
                if (index > newSyntaxOrder || index < 0)
                {
                    return list.Insert(i, syntaxToken);
                }
            }
        }

        return list.Add(syntaxToken);
    }

    private static int IndexOf(SyntaxToken token)
    {
        for (var i = 0; i < ModifiersSortOrder.Length; i++)
        {
            if (ModifiersSortOrder[i] == token.Text)
                return i;
        }

        return -1;
    }
}

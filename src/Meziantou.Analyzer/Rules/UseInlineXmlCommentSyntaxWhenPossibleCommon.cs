using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

internal static class UseInlineXmlCommentSyntaxWhenPossibleCommon
{
    /// <summary>
    /// Rewrites the element on a single line, or returns <see langword="null"/> when its content doesn't fit on a single line.
    /// </summary>
    /// <remarks>
    /// The analyzer and the code fixer both use this method, so the reported elements are exactly the ones the code fixer can rewrite,
    /// and the length computed by the analyzer is the length of the code the fixer produces.
    /// </remarks>
    public static XmlElementSyntax? CreateInlineElement(XmlElementSyntax elementSyntax)
    {
        if (!IsContentOnSingleLine(elementSyntax))
            return null;

        var content = new List<XmlNodeSyntax>(elementSyntax.Content.Count);
        foreach (var node in elementSyntax.Content)
        {
            // Nested elements, CDATA sections, comments and processing instructions are kept as-is, so their content is preserved
            if (node is not XmlTextSyntax textSyntax)
            {
                content.Add(RemoveExteriorTrivia(node));
                continue;
            }

            var tokens = new List<SyntaxToken>(textSyntax.TextTokens.Count);
            foreach (var token in textSyntax.TextTokens)
            {
                if (token.IsKind(SyntaxKind.XmlTextLiteralNewLineToken))
                    continue;

                tokens.Add(token.WithLeadingTrivia(RemoveExteriorTrivia(token.LeadingTrivia)).WithTrailingTrivia(RemoveExteriorTrivia(token.TrailingTrivia)));
            }

            if (tokens.Count > 0)
            {
                content.Add(textSyntax.WithTextTokens(TokenList(tokens)));
            }
        }

        // Remove the whitespace that separated the content from the tags
        if (content.Count > 0)
        {
            content[0] = TrimStart(content[0]);
            content[^1] = TrimEnd(content[^1]);
            content.RemoveAll(static node => node is XmlTextSyntax { TextTokens.Count: 0 });
        }

        var elementName = elementSyntax.StartTag.Name;

        return XmlElement(
            XmlElementStartTag(elementName, elementSyntax.StartTag.Attributes),
            List(content),
            XmlElementEndTag(elementName));
    }

    /// <summary>
    /// Determines whether all the content of the element is on a single line, ignoring the whitespace.
    /// </summary>
    private static bool IsContentOnSingleLine(XmlElementSyntax elementSyntax)
    {
        var contentLine = -1;
        foreach (var node in elementSyntax.Content)
        {
            if (node is XmlTextSyntax textSyntax)
            {
                foreach (var token in textSyntax.TextTokens)
                {
                    if (token.IsKind(SyntaxKind.XmlTextLiteralNewLineToken))
                        continue;

                    if (string.IsNullOrWhiteSpace(token.Text))
                        continue;

                    if (!IsOnContentLine(token.GetLocation(), ref contentLine))
                        return false;
                }
            }
            else if (!IsOnContentLine(node.GetLocation(), ref contentLine))
            {
                return false;
            }
        }

        return true;

        static bool IsOnContentLine(Location location, ref int contentLine)
        {
            var lineSpan = location.GetLineSpan();
            var startLine = lineSpan.StartLinePosition.Line;
            if (startLine != lineSpan.EndLinePosition.Line)
                return false;

            if (contentLine < 0)
            {
                contentLine = startLine;
                return true;
            }

            return contentLine == startLine;
        }
    }

    private static XmlNodeSyntax TrimStart(XmlNodeSyntax node)
    {
        if (node is not XmlTextSyntax textSyntax)
            return node;

        var tokens = textSyntax.TextTokens;
        if (tokens.Count == 0 || !tokens[0].IsKind(SyntaxKind.XmlTextLiteralToken))
            return node;

        return textSyntax.WithTextTokens(ReplaceText(tokens, index: 0, tokens[0].Text.TrimStart()));
    }

    private static XmlNodeSyntax TrimEnd(XmlNodeSyntax node)
    {
        if (node is not XmlTextSyntax textSyntax)
            return node;

        var tokens = textSyntax.TextTokens;
        if (tokens.Count == 0 || !tokens[^1].IsKind(SyntaxKind.XmlTextLiteralToken))
            return node;

        return textSyntax.WithTextTokens(ReplaceText(tokens, index: tokens.Count - 1, tokens[^1].Text.TrimEnd()));
    }

    private static SyntaxTokenList ReplaceText(SyntaxTokenList tokens, int index, string text)
    {
        var token = tokens[index];
        if (text.Length == 0)
            return tokens.RemoveAt(index);

        return tokens.Replace(token, XmlTextLiteral(text, text).WithTriviaFrom(token));
    }

    private static TNode RemoveExteriorTrivia<TNode>(TNode node)
        where TNode : SyntaxNode
    {
        var exteriorTrivia = node.DescendantTrivia().Where(static trivia => trivia.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia));
        return node.ReplaceTrivia(exteriorTrivia, static (_, _) => default);
    }

    private static IEnumerable<SyntaxTrivia> RemoveExteriorTrivia(SyntaxTriviaList trivia)
    {
        return trivia.Where(static item => !item.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia));
    }
}

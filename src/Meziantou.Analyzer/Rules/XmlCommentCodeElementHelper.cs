using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

/// <summary>
/// Finds the code elements ("c" and "code") of the documentation comments of the types and of their members.
/// </summary>
internal static class XmlCommentCodeElementHelper
{
    private static readonly ObjectPool<PooledQueue<SyntaxNode>> NodeQueuePool = ObjectPool.CreateQueuePool<SyntaxNode>();

    // Reserved keywords, plus the contextual keywords and the type keywords that are unlikely to be used as an identifier.
    // Contextual keywords such as "value", "from", "select", "add", "remove", "get" or "set" are excluded as they are commonly used as plain words or identifiers.
    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract",
        "as",
        "async",
        "await",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "dynamic",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "init",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "nameof",
        "namespace",
        "new",
        "nint",
        "nuint",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "partial",
        "private",
        "protected",
        "public",
        "readonly",
        "record",
        "ref",
        "required",
        "return",
        "sbyte",
        "scoped",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "var",
        "virtual",
        "void",
        "volatile",
        "while",
    };

    /// <summary>
    /// Calls <paramref name="analyzeCodeElement"/> for each code element of the documentation comments of the tree.
    /// </summary>
    public static void AnalyzeCodeElements(SyntaxTreeAnalysisContext context, Action<SyntaxTreeAnalysisContext, XmlElementSyntax> analyzeCodeElement)
    {
        // The documentation comments are not parsed, so they are regular comments
        if (context.Tree.Options.DocumentationMode is DocumentationMode.None)
            return;

        // Most documentation comments do not contain any code element, and searching the text of the file is much
        // faster than enumerating the documentation comments, as it does not create any syntax node
        var text = context.Tree.GetText(context.CancellationToken);
        using var searcher = new SourceTextSearcher(text, "<c", StringComparison.OrdinalIgnoreCase);

        SyntaxNode? root = null;
        var position = 0;
        while ((position = searcher.IndexOf(position)) >= 0)
        {
            var nextPosition = position + 2;
            if (IsCodeElementStartTag(text, nextPosition))
            {
                root ??= context.Tree.GetRoot(context.CancellationToken);
                var trivia = root.FindTrivia(position);
                if (IsDocumentationComment(trivia) && IsDocumentationCommentOfTypeOrMember(trivia) && trivia.GetStructure() is DocumentationCommentTriviaSyntax documentation)
                {
                    AnalyzeDocumentation(context, documentation, analyzeCodeElement);
                }

                // The text of a comment or of a disabled region can contain other matches, which do not need to be checked
                nextPosition = Math.Max(nextPosition, trivia.FullSpan.End);
            }

            position = nextPosition;
        }
    }

    /// <summary>Gets the keyword a code element consists of, which a "see" element must document with "langword".</summary>
    public static bool TryGetKeyword(XmlElementSyntax codeElement, [NotNullWhen(true)] out string? keyword)
    {
        if (codeElement.StartTag.Attributes.Count == 0 &&
            codeElement.Content.SingleOrDefaultIfMultiple() is XmlTextSyntax { TextTokens: [var codeText] } &&
            CSharpKeywords.Contains(codeText.Text))
        {
            keyword = codeText.Text;
            return true;
        }

        keyword = null;
        return false;
    }

    /// <summary>Indicates whether the attribute sets the language of a code element.</summary>
    public static bool IsLanguageAttribute(XmlAttributeSyntax attribute)
    {
        var attributeName = attribute.Name.LocalName.Text;
        return string.Equals(attributeName, "lang", StringComparison.OrdinalIgnoreCase) || string.Equals(attributeName, "language", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Indicates whether the attribute documents a keyword, so the element does not need a language.</summary>
    public static bool IsLangwordAttribute(XmlAttributeSyntax attribute)
    {
        return string.Equals(attribute.Name.LocalName.Text, "langword", StringComparison.OrdinalIgnoreCase);
    }

    // "<c" is followed by the end of the name of a code element
    private static bool IsCodeElementStartTag(SourceText text, int position)
    {
        if (text.Length - position >= 3 && text[position] is 'o' or 'O' && text[position + 1] is 'd' or 'D' && text[position + 2] is 'e' or 'E')
        {
            position += 3;
        }

        if (position >= text.Length)
            return false;

        var c = text[position];
        return c is '>' || char.IsWhiteSpace(c);
    }

    private static bool IsCodeElement(XmlElementSyntax element)
    {
        var name = element.StartTag.Name;
        if (name.Prefix is not null)
            return false;

        var elementName = name.LocalName.Text;
        return string.Equals(elementName, "c", StringComparison.OrdinalIgnoreCase) || string.Equals(elementName, "code", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDocumentationComment(SyntaxTrivia trivia)
    {
        return trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);
    }

    // The documentation comment of a declaration is in the leading trivia of its first token.
    // A documentation comment that is not placed on a type or on a member, such as the one of a local function, is not analyzed.
    private static bool IsDocumentationCommentOfTypeOrMember(SyntaxTrivia trivia)
    {
        var token = trivia.Token;
        var position = token.FullSpan.Start;
        for (var node = token.Parent; node is not null && node.FullSpan.Start == position; node = node.Parent)
        {
            if (node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or BaseMethodDeclarationSyntax or BaseFieldDeclarationSyntax or BasePropertyDeclarationSyntax or EnumMemberDeclarationSyntax or AccessorDeclarationSyntax)
                return true;
        }

        return false;
    }

    private static void AnalyzeDocumentation(SyntaxTreeAnalysisContext context, DocumentationCommentTriviaSyntax documentation, Action<SyntaxTreeAnalysisContext, XmlElementSyntax> analyzeCodeElement)
    {
        var queue = NodeQueuePool.Get();
        try
        {
            foreach (var item in documentation.ChildNodes())
            {
                queue.Enqueue(item);
            }

            while (queue.TryDequeue(out var childNode))
            {
                if (childNode is not XmlElementSyntax elementSyntax)
                    continue;

                if (IsCodeElement(elementSyntax))
                {
                    analyzeCodeElement(context, elementSyntax);
                }
                else
                {
                    foreach (var child in elementSyntax.Content)
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }
        finally
        {
            NodeQueuePool.Return(queue);
        }
    }
}

using Microsoft.CodeAnalysis.Formatting;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Adds the using directive required by the code fixes that call a method declared in a namespace that is not imported,
/// such as an extension method.
/// </summary>
internal static class UsingDirectiveHelper
{
    /// <summary>
    /// Adds a using directive for <paramref name="namespaceName"/> to the document of <paramref name="editor"/>, next to the using
    /// directives in scope at <paramref name="node"/>, unless the namespace is already imported there.
    /// </summary>
    public static void AddUsingDirective(DocumentEditor editor, SyntaxNode node, string namespaceName)
    {
        if (IsNamespaceImported(node, namespaceName))
            return;

        editor.ReplaceNode(GetUsingDirectivesContainer(node), (container, _) => AddUsingDirective(container, namespaceName));
    }

    /// <summary>
    /// Creates a copy of <paramref name="root"/> with a using directive for <paramref name="namespaceName"/> in scope at
    /// <paramref name="node"/>, which must be a node of <paramref name="root"/>.
    /// </summary>
    public static SyntaxNode AddUsingDirective(SyntaxNode root, SyntaxNode node, string namespaceName)
    {
        if (IsNamespaceImported(node, namespaceName))
            return root;

        var container = GetUsingDirectivesContainer(node);
        return root.ReplaceNode(container, AddUsingDirective(container, namespaceName));
    }

    public static bool IsNamespaceImported(SyntaxNode node, string namespaceName)
    {
        foreach (var ancestor in node.Ancestors())
        {
            var usings = ancestor switch
            {
                CompilationUnitSyntax compilationUnit => compilationUnit.Usings,
                BaseNamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.Usings,
                _ => default,
            };

            foreach (var usingDirective in usings)
            {
                if (IsNamespaceUsingDirective(usingDirective) && usingDirective.Name?.ToString() == namespaceName)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the node where the using directive is added: the innermost namespace declaration containing the node that already has using
    /// directives, or the compilation unit.
    /// </summary>
    public static SyntaxNode GetUsingDirectivesContainer(SyntaxNode node)
    {
        foreach (var ancestor in node.Ancestors())
        {
            if (ancestor is BaseNamespaceDeclarationSyntax { Usings.Count: > 0 })
                return ancestor;

            if (ancestor is CompilationUnitSyntax)
                return ancestor;
        }

        throw new InvalidOperationException("The node is not in a compilation unit");
    }

    public static SyntaxNode AddUsingDirective(SyntaxNode container, string namespaceName)
    {
        var endOfLine = container.DescendantTrivia().FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));
        if (endOfLine.IsKind(SyntaxKind.None))
        {
            endOfLine = SyntaxFactory.ElasticCarriageReturnLineFeed;
        }

        var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
            .NormalizeWhitespace()
            .WithTrailingTrivia(endOfLine)
            .WithAdditionalAnnotations(Formatter.Annotation);

        switch (container)
        {
            case CompilationUnitSyntax { Usings.Count: 0, Externs.Count: 0, Members: [var firstMember, ..] } compilationUnit:
                // The using directive becomes the first node of the file, so it takes the leading trivia of the first member, such as a file header
                usingDirective = usingDirective.WithLeadingTrivia(firstMember.GetLeadingTrivia()).WithTrailingTrivia(endOfLine, endOfLine);
                return compilationUnit
                    .ReplaceNode(firstMember, firstMember.WithoutLeadingTrivia())
                    .WithUsings(SyntaxFactory.SingletonList(usingDirective));

            case CompilationUnitSyntax compilationUnit:
                return compilationUnit.WithUsings(InsertUsingDirective(compilationUnit.Usings, usingDirective));

            case BaseNamespaceDeclarationSyntax namespaceDeclaration:
                return namespaceDeclaration.WithUsings(InsertUsingDirective(namespaceDeclaration.Usings, usingDirective));

            default:
                return container;
        }
    }

    /// <summary>
    /// Inserts the using directive with the other namespace using directives, at its sorted position when they are sorted
    /// (<c>System</c> namespaces first), or after them otherwise.
    /// </summary>
    private static SyntaxList<UsingDirectiveSyntax> InsertUsingDirective(SyntaxList<UsingDirectiveSyntax> usings, UsingDirectiveSyntax usingDirective)
    {
        var namespaceName = usingDirective.Name!.ToString();
        var firstIndex = -1;
        var lastIndex = -1;
        var isSorted = true;
        string? previousName = null;
        for (var i = 0; i < usings.Count; i++)
        {
            if (!IsNamespaceUsingDirective(usings[i]))
                continue;

            var name = usings[i].Name!.ToString();
            if (previousName is not null && CompareNamespaces(previousName, name) > 0)
            {
                isSorted = false;
            }

            if (firstIndex < 0)
            {
                firstIndex = i;
            }

            lastIndex = i;
            previousName = name;
        }

        int index;
        if (lastIndex < 0)
        {
            // Global using directives must be before the other using directives
            index = 0;
            while (index < usings.Count && !usings[index].GlobalKeyword.IsKind(SyntaxKind.None))
            {
                index++;
            }
        }
        else
        {
            index = lastIndex + 1;
            if (isSorted)
            {
                for (var i = firstIndex; i <= lastIndex; i++)
                {
                    if (IsNamespaceUsingDirective(usings[i]) && CompareNamespaces(namespaceName, usings[i].Name!.ToString()) < 0)
                    {
                        index = i;
                        break;
                    }
                }
            }
        }

        // Keep the leading trivia, such as a file header, at the beginning of the list
        if (index == 0 && usings.Count > 0)
        {
            usingDirective = usingDirective.WithLeadingTrivia(usings[0].GetLeadingTrivia());
            usings = usings.Replace(usings[0], usings[0].WithoutLeadingTrivia());
        }

        return usings.Insert(index, usingDirective);
    }

    private static bool IsNamespaceUsingDirective(UsingDirectiveSyntax usingDirective)
    {
        return usingDirective.GlobalKeyword.IsKind(SyntaxKind.None)
            && usingDirective.StaticKeyword.IsKind(SyntaxKind.None)
            && usingDirective.Alias is null
            && usingDirective.Name is not null;
    }

    private static int CompareNamespaces(string x, string y)
    {
        var xIsSystem = IsSystemNamespace(x);
        var yIsSystem = IsSystemNamespace(y);
        if (xIsSystem != yIsSystem)
            return xIsSystem ? -1 : 1;

        return StringComparer.OrdinalIgnoreCase.Compare(x, y);

        static bool IsSystemNamespace(string name) => name == "System" || name.StartsWith("System.", StringComparison.Ordinal);
    }
}

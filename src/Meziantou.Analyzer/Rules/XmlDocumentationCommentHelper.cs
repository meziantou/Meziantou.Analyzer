using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

internal static class XmlDocumentationCommentHelper
{
    /// <summary>
    /// Gets the node whose leading trivia contains the documentation comment of <paramref name="symbol"/>,
    /// or <see langword="null"/> when this documentation comment is shared with another symbol that analyzes it.
    /// </summary>
    public static SyntaxNode? GetDocumentedNode(ISymbol symbol, SyntaxNode declaringSyntax)
    {
        return declaringSyntax switch
        {
            // All the variables of a field declaration share its documentation comment, so it is analyzed with the first variable only
            VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: BaseFieldDeclarationSyntax fieldDeclaration } declaration } declarator
                => declaration.Variables[0] == declarator ? fieldDeclaration : null,

            // The primary constructor is declared by the type declaration, so its documentation comment is analyzed with the type
            TypeDeclarationSyntax when symbol is not INamedTypeSymbol => null,

            _ => declaringSyntax,
        };
    }
}

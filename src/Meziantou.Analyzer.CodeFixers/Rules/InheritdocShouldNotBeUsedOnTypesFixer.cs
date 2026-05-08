using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class InheritdocShouldNotBeUsedOnTypesFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.InheritdocShouldNotBeAmbiguousOnTypes);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true, findInsideTrivia: true);
        if (!TryGetInheritdocNode(nodeToFix, out var inheritdocNode, out var attributes))
            return;

        if (HasCrefAttribute(attributes))
            return;

        var typeDeclaration = nodeToFix?.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (typeDeclaration is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) is not INamedTypeSymbol typeSymbol)
            return;

        if (HasBaseType(typeSymbol) || typeSymbol.Interfaces.Length <= 1)
            return;

        if (inheritdocNode is XmlEmptyElementSyntax emptyElement)
        {
            RegisterCodeFixes(context, typeSymbol.Interfaces, emptyElement);
            return;
        }

        if (inheritdocNode is XmlElementStartTagSyntax startTag)
        {
            RegisterCodeFixes(context, typeSymbol.Interfaces, startTag);
        }
    }

    private static void RegisterCodeFixes(CodeFixContext context, ImmutableArray<INamedTypeSymbol> interfaces, XmlEmptyElementSyntax inheritdocNode)
    {
        foreach (var interfaceSymbol in interfaces)
        {
            RegisterCodeFix(context, interfaceSymbol, cancellationToken => AddCrefAttribute(context.Document, inheritdocNode, interfaceSymbol, cancellationToken));
        }
    }

    private static void RegisterCodeFixes(CodeFixContext context, ImmutableArray<INamedTypeSymbol> interfaces, XmlElementStartTagSyntax inheritdocNode)
    {
        foreach (var interfaceSymbol in interfaces)
        {
            RegisterCodeFix(context, interfaceSymbol, cancellationToken => AddCrefAttribute(context.Document, inheritdocNode, interfaceSymbol, cancellationToken));
        }
    }

    private static void RegisterCodeFix(CodeFixContext context, INamedTypeSymbol interfaceSymbol, Func<CancellationToken, Task<Document>> createChangedDocument)
    {
        var interfaceDisplayName = interfaceSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var crefValue = ToCrefValue(interfaceSymbol);
        var title = $"Add cref=\"{interfaceDisplayName}\"";
        var equivalenceKey = $"{title}:{crefValue}";

        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                createChangedDocument,
                equivalenceKey: equivalenceKey),
            context.Diagnostics);
    }

    private static async Task<Document> AddCrefAttribute(Document document, XmlEmptyElementSyntax inheritdocNode, INamedTypeSymbol interfaceSymbol, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var newNode = inheritdocNode.WithAttributes(inheritdocNode.Attributes.Add(XmlTextAttribute("cref", ToCrefValue(interfaceSymbol))));
        editor.ReplaceNode(inheritdocNode, newNode);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> AddCrefAttribute(Document document, XmlElementStartTagSyntax inheritdocNode, INamedTypeSymbol interfaceSymbol, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var newNode = inheritdocNode.WithAttributes(inheritdocNode.Attributes.Add(XmlTextAttribute("cref", ToCrefValue(interfaceSymbol))));
        editor.ReplaceNode(inheritdocNode, newNode);
        return editor.GetChangedDocument();
    }

    private static bool TryGetInheritdocNode(SyntaxNode? node, out SyntaxNode? inheritdocNode, out SyntaxList<XmlAttributeSyntax> attributes)
    {
        if (node?.FirstAncestorOrSelf<XmlEmptyElementSyntax>() is { } emptyElement && IsInheritdocElement(emptyElement.Name))
        {
            inheritdocNode = emptyElement;
            attributes = emptyElement.Attributes;
            return true;
        }

        if (node?.FirstAncestorOrSelf<XmlElementStartTagSyntax>() is { } startTag && IsInheritdocElement(startTag.Name))
        {
            inheritdocNode = startTag;
            attributes = startTag.Attributes;
            return true;
        }

        inheritdocNode = null;
        attributes = default;
        return false;
    }

    private static bool IsInheritdocElement(XmlNameSyntax name)
    {
        return string.Equals(name.LocalName.Text, "inheritdoc", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasCrefAttribute(SyntaxList<XmlAttributeSyntax> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (string.Equals(attribute.Name.LocalName.Text, "cref", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool HasBaseType(INamedTypeSymbol symbol)
    {
        return symbol.BaseType is { SpecialType: not (SpecialType.System_Object or SpecialType.System_ValueType) };
    }

    private static string ToCrefValue(INamedTypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                     .Replace("global::", "", StringComparison.Ordinal)
                     .Replace('<', '{')
                     .Replace('>', '}');
    }
}

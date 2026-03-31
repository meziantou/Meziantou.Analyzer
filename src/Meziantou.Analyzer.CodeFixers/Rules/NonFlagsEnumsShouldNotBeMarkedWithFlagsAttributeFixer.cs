using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class NonFlagsEnumsShouldNotBeMarkedWithFlagsAttributeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.NonFlagsEnumsShouldNotBeMarkedWithFlagsAttribute);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        var enumDeclaration = nodeToFix?.FirstAncestorOrSelf<EnumDeclarationSyntax>();
        if (enumDeclaration is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (semanticModel.Compilation.GetBestTypeByMetadataName("System.FlagsAttribute") is null)
            return;

        var title = "Remove [Flags] attribute";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                ct => RemoveFlagsAttribute(context.Document, enumDeclaration, ct),
                equivalenceKey: title),
            context.Diagnostics);
    }

    private static async Task<Document> RemoveFlagsAttribute(Document document, EnumDeclarationSyntax enumDeclaration, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || semanticModel is null)
            return document;

        var flagsAttributeSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.FlagsAttribute")!;

        var newAttributeLists = new List<AttributeListSyntax>(enumDeclaration.AttributeLists.Count);
        foreach (var attributeList in enumDeclaration.AttributeLists)
        {
            var attributes = new List<AttributeSyntax>(attributeList.Attributes.Count);
            foreach (var attribute in attributeList.Attributes)
            {
                var symbol = semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol as IMethodSymbol;
                if (symbol?.ContainingType.IsEqualTo(flagsAttributeSymbol) is true)
                    continue;

                attributes.Add(attribute);
            }

            if (attributes.Count > 0)
            {
                newAttributeLists.Add(attributeList.WithAttributes(SyntaxFactory.SeparatedList(attributes)));
            }
        }

        var updatedEnum = enumDeclaration.WithAttributeLists(SyntaxFactory.List(newAttributeLists));
        return document.WithSyntaxRoot(root.ReplaceNode(enumDeclaration, updatedEnum));
    }
}

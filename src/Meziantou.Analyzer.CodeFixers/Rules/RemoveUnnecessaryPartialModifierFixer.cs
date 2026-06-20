using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class RemoveUnnecessaryPartialModifierFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.RemoveUnnecessaryPartialModifier);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var partialKeyword = root.FindToken(context.Span.Start);
        if (!partialKeyword.IsKind(SyntaxKind.PartialKeyword))
            return;

        var typeDeclaration = partialKeyword.Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (typeDeclaration is null || !typeDeclaration.Modifiers.Contains(partialKeyword))
            return;

        var title = "Remove partial modifier";
        var codeAction = CodeAction.Create(
            title,
            ct => RemovePartialModifierAsync(context.Document, partialKeyword, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> RemovePartialModifierAsync(Document document, SyntaxToken partialKeyword, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var currentPartialKeyword = root.FindToken(partialKeyword.SpanStart);
        var typeDeclaration = currentPartialKeyword.Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (typeDeclaration is null || !typeDeclaration.Modifiers.Contains(currentPartialKeyword))
            return document;

        var newTypeDeclaration = RemovePartialModifier(typeDeclaration, currentPartialKeyword);
        return document.WithSyntaxRoot(root.ReplaceNode(typeDeclaration, newTypeDeclaration));
    }

    private static TypeDeclarationSyntax RemovePartialModifier(TypeDeclarationSyntax typeDeclaration, SyntaxToken partialKeyword)
    {
        var modifiers = typeDeclaration.Modifiers;
        var partialKeywordIndex = modifiers.IndexOf(partialKeyword);
        if (partialKeywordIndex < 0)
            return typeDeclaration;

        var nonspaceTrivia = partialKeyword.LeadingTrivia.Concat(partialKeyword.TrailingTrivia).Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)).ToList();

        if (partialKeywordIndex + 1 < modifiers.Count)
        {
            var nextModifier = modifiers[partialKeywordIndex + 1];
            modifiers = modifiers.Replace(nextModifier, nextModifier.WithLeadingTrivia(nonspaceTrivia.Concat(nextModifier.LeadingTrivia)));
            return typeDeclaration.WithModifiers(modifiers.Remove(partialKeyword));
        }

        return typeDeclaration.WithModifiers(modifiers.Remove(partialKeyword));
    }
}

#if ROSLYN_5_9_OR_GREATER
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class RemoveUnnecessaryClosedModifierFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.RemoveUnnecessaryClosedModifier);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var closedKeyword = root.FindToken(context.Span.Start);
#pragma warning disable RSEXPERIMENTAL006
        if (!closedKeyword.IsKind(SyntaxKind.ClosedKeyword))
            return;
#pragma warning restore RSEXPERIMENTAL006

        if (closedKeyword.Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault() is not { } typeDeclaration || !typeDeclaration.Modifiers.Contains(closedKeyword))
            return;

        var title = "Remove closed modifier";
        var codeAction = CodeAction.Create(
            title,
            ct => RemoveClosedModifierAsync(context.Document, closedKeyword, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> RemoveClosedModifierAsync(Document document, SyntaxToken closedKeyword, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var currentClosedKeyword = root.FindToken(closedKeyword.SpanStart);
        if (currentClosedKeyword.Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault() is not { } typeDeclaration || !typeDeclaration.Modifiers.Contains(currentClosedKeyword))
            return document;

        var newTypeDeclaration = RemoveClosedModifier(typeDeclaration, currentClosedKeyword);
        return document.WithSyntaxRoot(root.ReplaceNode(typeDeclaration, newTypeDeclaration));
    }

    private static TypeDeclarationSyntax RemoveClosedModifier(TypeDeclarationSyntax typeDeclaration, SyntaxToken closedKeyword)
    {
        var modifiers = typeDeclaration.Modifiers;
        var closedKeywordIndex = modifiers.IndexOf(closedKeyword);
        if (closedKeywordIndex < 0)
            return typeDeclaration;

        // Keep the leading trivia (indentation, comments) and the comments located after the modifier
        var triviaToMove = closedKeyword.LeadingTrivia
            .Concat(closedKeyword.TrailingTrivia.Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))
            .ToList();

        modifiers = modifiers.RemoveAt(closedKeywordIndex);
        if (closedKeywordIndex < modifiers.Count)
        {
            var nextModifier = modifiers[closedKeywordIndex];
            return typeDeclaration.WithModifiers(modifiers.Replace(nextModifier, nextModifier.WithLeadingTrivia(triviaToMove.Concat(nextModifier.LeadingTrivia))));
        }

        return typeDeclaration
            .WithModifiers(modifiers)
            .WithKeyword(typeDeclaration.Keyword.WithLeadingTrivia(triviaToMove.Concat(typeDeclaration.Keyword.LeadingTrivia)));
    }
}
#endif

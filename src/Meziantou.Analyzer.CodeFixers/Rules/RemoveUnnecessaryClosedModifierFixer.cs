#if ROSLYN_5_9_OR_GREATER
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

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) is not { } symbol)
            return;

        // A closed type is implicitly abstract, so it may have unimplemented abstract members.
        // In that case, the type cannot become a concrete type and the only option is to make it explicitly abstract.
        if (MustRemainAbstract(symbol))
        {
            RegisterReplaceModifier(context, closedKeyword, SyntaxKind.AbstractKeyword, "Replace closed modifier with abstract");
            return;
        }

        var removeTitle = "Remove closed modifier";
        context.RegisterCodeFix(
            CodeAction.Create(removeTitle, ct => RemoveClosedModifierAsync(context.Document, closedKeyword, ct), equivalenceKey: removeTitle),
            context.Diagnostics);

        // 'sealed' preserves the intent of 'closed': no other type can derive from it
        RegisterReplaceModifier(context, closedKeyword, SyntaxKind.SealedKeyword, "Replace closed modifier with sealed");
    }

    private static void RegisterReplaceModifier(CodeFixContext context, SyntaxToken closedKeyword, SyntaxKind modifier, string title)
    {
        context.RegisterCodeFix(
            CodeAction.Create(title, ct => ReplaceClosedModifierAsync(context.Document, closedKeyword, modifier, ct), equivalenceKey: title),
            context.Diagnostics);
    }

    private static bool MustRemainAbstract(INamedTypeSymbol symbol)
    {
        var overriddenMembers = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var member in symbol.GetAllMembers())
        {
            var overriddenMember = member switch
            {
                IMethodSymbol method => (ISymbol?)method.OverriddenMethod,
                IPropertySymbol property => property.OverriddenProperty,
                IEventSymbol @event => @event.OverriddenEvent,
                _ => null,
            };

            if (overriddenMember is not null)
            {
                overriddenMembers.Add(overriddenMember);
            }
        }

        // Records declare an implicit abstract '<Clone>$' method when they are abstract, so implicitly declared members must be ignored
        foreach (var member in symbol.GetAllMembers())
        {
            if (member.IsAbstract && !member.IsImplicitlyDeclared && !overriddenMembers.Contains(member))
                return true;
        }

        return false;
    }

    private static async Task<Document> ReplaceClosedModifierAsync(Document document, SyntaxToken closedKeyword, SyntaxKind modifier, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var currentClosedKeyword = root.FindToken(closedKeyword.SpanStart);
        var newModifier = SyntaxFactory.Token(modifier).WithTriviaFrom(currentClosedKeyword);
        return document.WithSyntaxRoot(root.ReplaceToken(currentClosedKeyword, newModifier));
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

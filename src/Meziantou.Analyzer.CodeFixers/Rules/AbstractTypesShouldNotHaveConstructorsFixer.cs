using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class AbstractTypesShouldNotHaveConstructorsFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.AbstractTypesShouldNotHaveConstructors);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not ConstructorDeclarationSyntax ctorSyntax)
            return;

        if (!ctorSyntax.Modifiers.Any(SyntaxKind.PublicKeyword) && !ctorSyntax.Modifiers.Any(SyntaxKind.InternalKeyword))
            return;

        var title = "Make constructor protected";
        var codeAction = CodeAction.Create(
            title,
            ct => MakeConstructorProtected(context.Document, ctorSyntax, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> MakeConstructorProtected(Document document, ConstructorDeclarationSyntax ctorSyntax, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // Replace the accessibility modifier in place to keep its trivia (e.g. the XML documentation of the constructor)
        var modifiers = ctorSyntax.Modifiers;
        var index = modifiers.IndexOf(SyntaxKind.PublicKeyword);
        if (index < 0)
        {
            index = modifiers.IndexOf(SyntaxKind.InternalKeyword);
        }

        var accessibilityModifier = modifiers[index];
        modifiers = modifiers.Replace(accessibilityModifier, SyntaxFactory.Token(accessibilityModifier.LeadingTrivia, SyntaxKind.ProtectedKeyword, accessibilityModifier.TrailingTrivia));

        editor.ReplaceNode(ctorSyntax, ctorSyntax.WithModifiers(modifiers));
        return editor.GetChangedDocument();
    }
}

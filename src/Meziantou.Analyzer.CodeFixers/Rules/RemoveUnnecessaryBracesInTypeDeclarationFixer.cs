using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class RemoveUnnecessaryBracesInTypeDeclarationFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.RemoveUnnecessaryBracesInTypeDeclaration);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var typeDeclaration = root?.FindNode(context.Span, getInnermostNodeForTie: true).FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (typeDeclaration is null || !CanRemoveBraces(typeDeclaration))
            return;

        const string Title = "Remove unnecessary braces";
        var codeAction = CodeAction.Create(
            Title,
            ct => RemoveBraces(context.Document, typeDeclaration, ct),
            equivalenceKey: Title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> RemoveBraces(Document document, TypeDeclarationSyntax typeDeclaration, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semicolonToken = SyntaxFactory.Token(SyntaxKind.SemicolonToken)
            .WithTrailingTrivia(typeDeclaration.CloseBraceToken.TrailingTrivia);

        var newNode = typeDeclaration
            .WithOpenBraceToken(default)
            .WithCloseBraceToken(default)
            .WithSemicolonToken(semicolonToken)
            .WithAdditionalAnnotations(Formatter.Annotation);

        editor.ReplaceNode(typeDeclaration, newNode);
        return editor.GetChangedDocument();
    }

    private static bool CanRemoveBraces(TypeDeclarationSyntax typeDeclaration)
    {
        if (!CanUseSemicolonTypeDeclaration(typeDeclaration))
            return false;

        if (typeDeclaration.Members.Count != 0)
            return false;

        if (typeDeclaration.OpenBraceToken.IsMissing || typeDeclaration.CloseBraceToken.IsMissing || typeDeclaration.SemicolonToken.IsKind(SyntaxKind.SemicolonToken))
            return false;

        return !ContainsCommentOrDirectiveInBraces(typeDeclaration);
    }

    private static bool CanUseSemicolonTypeDeclaration(TypeDeclarationSyntax typeDeclaration)
    {
        if (typeDeclaration is RecordDeclarationSyntax)
            return true;

#if CSHARP12_OR_GREATER
        if (!typeDeclaration.GetCSharpLanguageVersion().IsCSharp12OrAbove())
            return false;

        if (typeDeclaration is ClassDeclarationSyntax or StructDeclarationSyntax)
            return true;
#endif

        return false;
    }

    private static bool ContainsCommentOrDirectiveInBraces(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.OpenBraceToken.TrailingTrivia
            .Concat(typeDeclaration.CloseBraceToken.LeadingTrivia)
            .Any(static trivia => trivia.IsDirective || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia));
    }
}

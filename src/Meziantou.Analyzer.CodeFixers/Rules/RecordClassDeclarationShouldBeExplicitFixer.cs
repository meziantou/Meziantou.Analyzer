#if CSHARP10_OR_GREATER
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class RecordClassDeclarationShouldBeExplicitFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.RecordClassDeclarationShouldBeExplicit);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not RecordDeclarationSyntax nodeToFix)
            return;

        var title = "Add 'class' keyword";
        var codeAction = CodeAction.Create(
            title,
            ct => AddClassKeyword(context.Document, nodeToFix, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> AddClassKeyword(Document document, RecordDeclarationSyntax nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var classKeyword = SyntaxFactory.Token(SyntaxKind.ClassKeyword)
            .WithLeadingTrivia(nodeToFix.Keyword.TrailingTrivia)
            .WithTrailingTrivia(SyntaxFactory.Space);

        var newNode = nodeToFix
            .WithKeyword(nodeToFix.Keyword.WithTrailingTrivia())
            .WithClassOrStructKeyword(classKeyword);

        editor.ReplaceNode(nodeToFix, newNode);
        return editor.GetChangedDocument();
    }
}
#endif

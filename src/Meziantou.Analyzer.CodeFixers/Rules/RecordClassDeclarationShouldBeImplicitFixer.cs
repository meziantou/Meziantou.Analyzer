#if CSHARP10_OR_GREATER
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class RecordClassDeclarationShouldBeImplicitFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.RecordClassDeclarationShouldBeImplicit);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not RecordDeclarationSyntax recordDeclaration)
            return;

        var title = "Remove 'class' keyword";
        var codeAction = CodeAction.Create(
            title,
            ct => Fix(context.Document, recordDeclaration, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> Fix(Document document, RecordDeclarationSyntax recordDeclaration, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var classKeyword = recordDeclaration.ClassOrStructKeyword;
        var newRecordKeyword = recordDeclaration.Keyword.WithTrailingTrivia(classKeyword.TrailingTrivia);
        var newRecordDeclaration = recordDeclaration
            .WithKeyword(newRecordKeyword)
            .WithClassOrStructKeyword(default);

        editor.ReplaceNode(recordDeclaration, newRecordDeclaration);
        return editor.GetChangedDocument();
    }
}
#endif

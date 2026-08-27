using Microsoft.CodeAnalysis.Formatting;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class RemoveEmptyBlockFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.RemoveEmptyBlock);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        // The analyzer reports the diagnostic on the clause itself, so the node kind determines the fix.
        // Walking up the ancestors would pick the enclosing else clause of a nested finally clause.
        switch (root?.FindNode(context.Span, getInnermostNodeForTie: true))
        {
            case ElseClauseSyntax { Parent: IfStatementSyntax ifStatement }:
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Remove empty else block",
                        ct => RemoveElseClause(context.Document, ifStatement, ct),
                        equivalenceKey: "Remove empty else block"),
                    context.Diagnostics);
                break;

            case FinallyClauseSyntax { Parent: TryStatementSyntax tryStatement }:
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Remove empty finally block",
                        ct => RemoveFinallyClause(context.Document, tryStatement, ct),
                        equivalenceKey: "Remove empty finally block"),
                    context.Diagnostics);
                break;
        }
    }

    private static async Task<Document> RemoveElseClause(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(ifStatement, ifStatement.WithElse(null).WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static async Task<Document> RemoveFinallyClause(Document document, TryStatementSyntax tryStatement, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (tryStatement.Catches.Count > 0)
        {
            editor.ReplaceNode(tryStatement, tryStatement.WithFinally(null).WithAdditionalAnnotations(Formatter.Annotation));
        }
        else
        {
            editor.ReplaceNode(tryStatement, tryStatement.Block.WithTriviaFrom(tryStatement).WithAdditionalAnnotations(Formatter.Annotation));
        }

        return editor.GetChangedDocument();
    }
}

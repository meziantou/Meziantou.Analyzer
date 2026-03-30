using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
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
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        if (nodeToFix is ElseClauseSyntax || nodeToFix.AncestorsAndSelf().OfType<ElseClauseSyntax>().FirstOrDefault() is not null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Remove empty else block",
                    ct => RemoveElseClause(context.Document, nodeToFix, ct),
                    equivalenceKey: "Remove empty else block"),
                context.Diagnostics);
        }
        else if (nodeToFix is FinallyClauseSyntax || nodeToFix.AncestorsAndSelf().OfType<FinallyClauseSyntax>().FirstOrDefault() is not null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Remove empty finally block",
                    ct => RemoveFinallyClause(context.Document, nodeToFix, ct),
                    equivalenceKey: "Remove empty finally block"),
                context.Diagnostics);
        }
    }

    private static async Task<Document> RemoveElseClause(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var elseClause = nodeToFix as ElseClauseSyntax ?? nodeToFix.AncestorsAndSelf().OfType<ElseClauseSyntax>().FirstOrDefault();
        if (elseClause?.Parent is not IfStatementSyntax ifStatement)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(ifStatement, ifStatement.WithElse(null).WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static async Task<Document> RemoveFinallyClause(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var finallyClause = nodeToFix as FinallyClauseSyntax ?? nodeToFix.AncestorsAndSelf().OfType<FinallyClauseSyntax>().FirstOrDefault();
        if (finallyClause?.Parent is not TryStatementSyntax tryStatement)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(tryStatement, tryStatement.WithFinally(null).WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }
}

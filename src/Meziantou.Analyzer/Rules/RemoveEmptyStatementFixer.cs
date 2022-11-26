using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class RemoveEmptyStatementFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.RemoveEmptyStatement);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not StatementSyntax statementSyntax)
            return;

        if (ShouldReplaceWithEmptyBlock(statementSyntax))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use empty block",
                    ct => ReplaceWithEmptyBlock(context.Document, statementSyntax, ct),
                    equivalenceKey: "Use empty block"),
                context.Diagnostics);
        }
        else
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Remove empty statement",
                    ct => Remove(context.Document, statementSyntax, ct),
                    equivalenceKey: "Remove empty statement"),
                context.Diagnostics);
        }
    }

    private static async Task<Document> Remove(Document document, StatementSyntax statementSyntax, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.RemoveNode(statementSyntax);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> ReplaceWithEmptyBlock(Document document, StatementSyntax statementSyntax, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(statementSyntax, SyntaxFactory.Block());
        return editor.GetChangedDocument();
    }

    private bool ShouldReplaceWithEmptyBlock(StatementSyntax statementSyntax)
    {
        var parent = statementSyntax.Parent;
        if (parent.IsKind(SyntaxKind.WhileStatement))
        {
            if (((WhileStatementSyntax)parent).Statement == statementSyntax)
                return true;
        }
        else if (parent.IsKind(SyntaxKind.ForStatement))
        {
            if (((ForStatementSyntax)parent).Statement == statementSyntax)
                return true;
        }

        else if (parent.IsKind(SyntaxKind.ForEachStatement))
        {
            if (((ForEachStatementSyntax)parent).Statement == statementSyntax)
                return true;
        }

        return false;
    }
}

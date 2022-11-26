using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class RemoveUselessToStringFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.RemoveUselessToString);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not InvocationExpressionSyntax syntax)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Remove ToString",
                ct => Remove(context.Document, syntax, ct),
                equivalenceKey: "Remove ToString"),
            context.Diagnostics);
    }

    private static async Task<Document> Remove(Document document, InvocationExpressionSyntax expression, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        if (expression.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            editor.ReplaceNode(expression, memberAccessExpressionSyntax.Expression);
        }

        return editor.GetChangedDocument();
    }
}

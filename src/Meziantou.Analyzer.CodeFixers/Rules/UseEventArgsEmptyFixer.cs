using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseEventArgsEmptyFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseEventArgsEmpty, RuleIdentifiers.EventArgsSenderShouldNotBeNullForEvents);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use EventArgs.Empty",
                ct => Fix(context, nodeToFix, ct),
                equivalenceKey: "Use EventArgs.Empty"),
            context.Diagnostics);
    }

    private static async Task<Document> Fix(CodeFixContext context, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var typeSymbol = editor.SemanticModel.Compilation.GetBestTypeByMetadataName("System.EventArgs");
        if (typeSymbol is null)
            return context.Document;

        var newExpression = generator.MemberAccessExpression(generator.TypeExpression(typeSymbol), nameof(EventArgs.Empty));
        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }
}

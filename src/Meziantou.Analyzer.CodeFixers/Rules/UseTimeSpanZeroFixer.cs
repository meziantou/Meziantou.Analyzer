using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseTimeSpanZeroFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseTimeSpanZero);

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

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var timeSpanType = semanticModel.Compilation.GetBestTypeByMetadataName("System.TimeSpan");
        if (timeSpanType is null)
            return;

        var title = "Use TimeSpan.Zero";
        var codeAction = CodeAction.Create(
            title,
            ct => ConvertToTimeSpanZero(context.Document, nodeToFix, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> ConvertToTimeSpanZero(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        var generator = editor.Generator;

        editor.ReplaceNode(nodeToFix, GenerateTimeSpanZeroExpression(generator, semanticModel).WithTriviaFrom(nodeToFix));
        return editor.GetChangedDocument();
    }

    private static SyntaxNode GenerateTimeSpanZeroExpression(SyntaxGenerator generator, SemanticModel semanticModel)
    {
        var timeSpanType = semanticModel.Compilation.GetBestTypeByMetadataName("System.TimeSpan");
        return generator.MemberAccessExpression(
            generator.TypeExpression(timeSpanType!),
            nameof(TimeSpan.Zero));
    }
}

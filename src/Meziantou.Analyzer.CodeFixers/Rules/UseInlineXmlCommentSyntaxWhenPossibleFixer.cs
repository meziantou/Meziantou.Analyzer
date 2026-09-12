namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseInlineXmlCommentSyntaxWhenPossibleFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseSingleLineXmlCommentSyntaxWhenPossible);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true, findInsideTrivia: true);
        if (nodeToFix is not XmlElementSyntax elementSyntax)
            return;

        var inlineElement = UseInlineXmlCommentSyntaxWhenPossibleCommon.CreateInlineElement(elementSyntax);
        if (inlineElement is null)
            return;

        var title = "Use single-line XML comment syntax";
        var codeAction = CodeAction.Create(
            title,
            cancellationToken => Fix(context.Document, elementSyntax, inlineElement, cancellationToken),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> Fix(Document document, XmlElementSyntax elementSyntax, XmlElementSyntax inlineElement, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var newNode = inlineElement
            .WithLeadingTrivia(elementSyntax.GetLeadingTrivia())
            .WithTrailingTrivia(elementSyntax.GetTrailingTrivia());

        editor.ReplaceNode(elementSyntax, newNode);
        return editor.GetChangedDocument();
    }
}

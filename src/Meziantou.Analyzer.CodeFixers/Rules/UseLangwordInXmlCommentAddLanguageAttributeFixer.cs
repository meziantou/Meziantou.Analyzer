using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseLangwordInXmlCommentAddLanguageAttributeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseLangwordInXmlComment, RuleIdentifiers.MissingLanguageAttributeInXmlComment);

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true, findInsideTrivia: true);
        if (nodeToFix is not XmlElementSyntax elementSyntax)
            return;

        if (elementSyntax.StartTag.Attributes.Count > 0)
            return;

        const string Title = "Add language attribute";
        var codeAction = CodeAction.Create(
            Title,
            cancellationToken => Fix(context.Document, elementSyntax, cancellationToken),
            equivalenceKey: Title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> Fix(Document document, XmlElementSyntax elementSyntax, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var attribute = XmlTextAttribute("language").WithLeadingTrivia(Whitespace(" "));
        editor.ReplaceNode(elementSyntax.StartTag, elementSyntax.StartTag.AddAttributes(attribute));
        return editor.GetChangedDocument();
    }
}

using System.Collections.Immutable;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseInlineXmlCommentSyntaxWhenPossibleFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseInlineXmlCommentSyntaxWhenPossible);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true, findInsideTrivia: true);
        if (nodeToFix is not XmlElementSyntax elementSyntax)
            return;

        var title = "Use inline XML comment syntax";
        var codeAction = CodeAction.Create(
            title,
            cancellationToken => Fix(context.Document, elementSyntax, cancellationToken),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> Fix(Document document, XmlElementSyntax elementSyntax, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // Extract the text content
        var contentText = new StringBuilder();
        foreach (var content in elementSyntax.Content)
        {
            if (content is XmlTextSyntax textSyntax)
            {
                foreach (var token in textSyntax.TextTokens)
                {
                    // Skip newline tokens
                    if (token.IsKind(SyntaxKind.XmlTextLiteralNewLineToken))
                        continue;

                    var text = token.Text.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        if (contentText.Length > 0)
                            contentText.Append(' ');
                        contentText.Append(text);
                    }
                }
            }
        }

        // Create inline syntax
        var elementName = elementSyntax.StartTag.Name;
        var attributes = elementSyntax.StartTag.Attributes;

        var newNode = XmlElement(
            XmlElementStartTag(elementName, attributes),
            SingletonList<XmlNodeSyntax>(XmlText(contentText.ToString())),
            XmlElementEndTag(elementName))
            .WithLeadingTrivia(elementSyntax.GetLeadingTrivia())
            .WithTrailingTrivia(elementSyntax.GetTrailingTrivia());

        editor.ReplaceNode(elementSyntax, newNode);
        return editor.GetChangedDocument();
    }
}

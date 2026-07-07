using System.Collections.Immutable;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseMultiLineXmlCommentSyntaxFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseMultiLineXmlCommentSyntax);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true, findInsideTrivia: true);
        if (nodeToFix is not XmlElementSyntax elementSyntax)
            return;

        var sourceText = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
        if (!TryCreateReplacementText(sourceText, elementSyntax, out var replacementText))
            return;

        var title = "Use multi-line XML comment syntax";
        var codeAction = CodeAction.Create(
            title,
            cancellationToken => FixAsync(context.Document, elementSyntax.Span, replacementText, cancellationToken),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static bool TryCreateReplacementText(SourceText sourceText, XmlElementSyntax elementSyntax, out string replacementText)
    {
        replacementText = string.Empty;

        var lineSpan = elementSyntax.GetLocation().GetLineSpan();
        if (lineSpan.StartLinePosition.Line != lineSpan.EndLinePosition.Line)
            return false;

        var line = sourceText.Lines.GetLineFromPosition(elementSyntax.SpanStart);
        var commentPrefix = sourceText.ToString(TextSpan.FromBounds(line.Start, elementSyntax.SpanStart));
        var lineBreak = sourceText.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
        if (lineBreak.Length == 0)
        {
            lineBreak = "\n";
        }

        var contentText = new StringBuilder();
        foreach (var content in elementSyntax.Content)
        {
            if (content is not XmlTextSyntax textSyntax)
                continue;

            foreach (var token in textSyntax.TextTokens)
            {
                if (token.IsKind(SyntaxKind.XmlTextLiteralNewLineToken))
                    continue;

                var text = token.Text.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (contentText.Length > 0)
                {
                    contentText.Append(' ');
                }

                contentText.Append(text);
            }
        }

        var startTagText = elementSyntax.StartTag.ToString();
        var endTagText = elementSyntax.EndTag.ToString();
        replacementText = contentText.Length == 0
            ? startTagText + lineBreak + commentPrefix + endTagText
            : startTagText + lineBreak + commentPrefix + contentText + lineBreak + commentPrefix + endTagText;
        return true;
    }

    private static async Task<Document> FixAsync(Document document, TextSpan span, string replacementText, CancellationToken cancellationToken)
    {
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return document.WithText(sourceText.Replace(span, replacementText));
    }
}

using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class DoNotUseNonAsciiCharactersFixer : CodeFixProvider
{
    private const string Title = "Use escape sequences";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.DoNotUseNonAsciiCharacters);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var token = root.FindToken(context.Span.Start, findInsideTrivia: true);
        if (!token.Span.Contains(context.Span) || !SupportsEscapeSequences(token))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => UseEscapeSequences(context.Document, context.Span, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static bool SupportsEscapeSequences(SyntaxToken token)
    {
        return token.Kind() switch
        {
            SyntaxKind.StringLiteralToken or SyntaxKind.Utf8StringLiteralToken => token.Text is not ['@', ..],
            SyntaxKind.CharacterLiteralToken => true,
            SyntaxKind.InterpolatedStringTextToken => token.Parent is InterpolatedStringTextSyntax { Parent: InterpolatedStringExpressionSyntax interpolatedString } && interpolatedString.StringStartToken.IsKind(SyntaxKind.InterpolatedStringStartToken),
            _ => false,
        };
    }

    private static async Task<Document> UseEscapeSequences(Document document, TextSpan span, CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var value = text.ToString(span);
        var escaped = new StringBuilder(value.Length * 6);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
            {
                escaped.Append("\\U").Append(char.ConvertToUtf32(c, value[i + 1]).ToString("X8", CultureInfo.InvariantCulture));
                i++;
            }
            else
            {
                escaped.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
            }
        }

        return document.WithText(text.WithChanges(new TextChange(span, escaped.ToString())));
    }
}

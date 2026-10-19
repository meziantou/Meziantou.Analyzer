using System.Text;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseEscapeSequenceForInvisibleCharacterFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseEscapeSequenceForInvisibleCharacter);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        // The whole literal is rewritten, so the fixes of all the characters of a literal are identical and can be merged by the FixAll
        var token = root.FindToken(context.Span.Start);
        var (nodeToFix, newNode) = token.Parent switch
        {
            LiteralExpressionSyntax literal => ((ExpressionSyntax)literal, (ExpressionSyntax?)EscapeLiteral(literal)),
            InterpolatedStringTextSyntax { Parent: InterpolatedStringExpressionSyntax interpolatedString } => (interpolatedString, EscapeInterpolatedString(interpolatedString)),
            _ => default,
        };

        if (nodeToFix is null || newNode is null)
            return;

        var title = "Use escape sequences";
        var codeAction = CodeAction.Create(
            title,
            _ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(nodeToFix, newNode))),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static LiteralExpressionSyntax? EscapeLiteral(LiteralExpressionSyntax literal)
    {
        // Raw strings cannot contain escape sequences
        var token = literal.Token;
        if (!token.IsKind(SyntaxKind.StringLiteralToken) && !token.IsKind(SyntaxKind.Utf8StringLiteralToken) && !token.IsKind(SyntaxKind.CharacterLiteralToken))
            return null;

        var text = token.Text;
        string newText;
        if (text.StartsWith('@', StringComparison.Ordinal))
        {
            var suffix = token.IsKind(SyntaxKind.Utf8StringLiteralToken) ? text[^2..] : "";
            var sb = new StringBuilder(text.Length);
            sb.Append('"');
            AppendVerbatimText(sb, text[2..(text.Length - suffix.Length - 1)]);
            sb.Append('"');
            sb.Append(suffix);
            newText = sb.ToString();
        }
        else
        {
            var sb = new StringBuilder(text.Length);
            AppendRegularText(sb, text);
            newText = sb.ToString();
        }

        var newToken = SyntaxFactory.ParseToken(newText);
        if (!newToken.IsKind(token.Kind()) || newToken.ContainsDiagnostics || newToken.Span.Length != newText.Length)
            return null;

        return literal.WithToken(newToken.WithTriviaFrom(token));
    }

    private static InterpolatedStringExpressionSyntax? EscapeInterpolatedString(InterpolatedStringExpressionSyntax interpolatedString)
    {
        var isVerbatim = interpolatedString.StringStartToken.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken);
        if (!isVerbatim && !interpolatedString.StringStartToken.IsKind(SyntaxKind.InterpolatedStringStartToken))
            return null;

        var sb = new StringBuilder();
        sb.Append(isVerbatim ? "$\"" : interpolatedString.StringStartToken.Text);
        foreach (var content in interpolatedString.Contents)
        {
            switch (content)
            {
                case InterpolatedStringTextSyntax text when isVerbatim:
                    AppendVerbatimText(sb, text.TextToken.Text);
                    break;

                case InterpolatedStringTextSyntax text:
                    AppendRegularText(sb, text.TextToken.Text);
                    break;

                case InterpolationSyntax interpolation:
                    // The escape sequences of the format clause are not interpreted the same way in verbatim and regular strings
                    if (isVerbatim && interpolation.FormatClause?.FormatStringToken.Text is { } format && (format.Contains('\\', StringComparison.Ordinal) || format.Contains('"', StringComparison.Ordinal)))
                        return null;

                    sb.Append(interpolation.ToFullString());
                    break;
            }
        }

        sb.Append(interpolatedString.StringEndToken.Text);

        var newText = sb.ToString();
        var newNode = SyntaxFactory.ParseExpression(newText, options: interpolatedString.SyntaxTree.Options);
        if (newNode is not InterpolatedStringExpressionSyntax newInterpolatedString || newNode.ContainsDiagnostics || newNode.FullSpan.Length != newText.Length)
            return null;

        return newInterpolatedString.WithTriviaFrom(interpolatedString);
    }

    private static void AppendRegularText(StringBuilder sb, string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var codePoint = UseEscapeSequenceForInvisibleCharacterCommon.GetCodePoint(text, i, out var length);
            if (UseEscapeSequenceForInvisibleCharacterCommon.IsInvisibleCharacter(codePoint))
            {
                sb.Append(UseEscapeSequenceForInvisibleCharacterCommon.GetEscapeSequence(codePoint));
            }
            else
            {
                sb.Append(text, i, length);
            }

            i += length - 1;
        }
    }

    private static void AppendVerbatimText(StringBuilder sb, string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var codePoint = UseEscapeSequenceForInvisibleCharacterCommon.GetCodePoint(text, i, out var length);
            switch (codePoint)
            {
                case '"':
                    // Quotes are doubled in verbatim strings
                    sb.Append("\\\"");
                    i++;
                    break;

                case '\\':
                    sb.Append("\\\\");
                    break;

                case '\r':
                    sb.Append("\\r");
                    break;

                case '\n':
                    sb.Append("\\n");
                    break;

                case var _ when UseEscapeSequenceForInvisibleCharacterCommon.IsInvisibleCharacter(codePoint):
                    sb.Append(UseEscapeSequenceForInvisibleCharacterCommon.GetEscapeSequence(codePoint));
                    break;

                default:
                    sb.Append(text, i, length);
                    break;
            }

            i += length - 1;
        }
    }
}

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseEscapeSequenceForInvisibleCharacterAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseEscapeSequenceForInvisibleCharacter,
        title: "Use an escape sequence for invisible characters",
        messageFormat: "Replace the invisible character '{0}' with an escape sequence",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseEscapeSequenceForInvisibleCharacter));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeLiteralExpression, SyntaxKind.StringLiteralExpression, SyntaxKind.Utf8StringLiteralExpression, SyntaxKind.CharacterLiteralExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInterpolatedString, SyntaxKind.InterpolatedStringExpression);
    }

    private static void AnalyzeLiteralExpression(SyntaxNodeAnalysisContext context)
    {
        var token = ((LiteralExpressionSyntax)context.Node).Token;
        var rawString = token.Kind() is SyntaxKind.MultiLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken
            ? MultiLineRawString.Create(context.Node, context.CancellationToken)
            : null;

        AnalyzeText(context, token.Text, token.SpanStart, rawString);
    }

    private static void AnalyzeInterpolatedString(SyntaxNodeAnalysisContext context)
    {
        var node = (InterpolatedStringExpressionSyntax)context.Node;
        var rawString = node.StringStartToken.IsKind(SyntaxKind.InterpolatedMultiLineRawStringStartToken)
            ? MultiLineRawString.Create(node, context.CancellationToken)
            : null;

        foreach (var content in node.Contents)
        {
            if (content is InterpolatedStringTextSyntax text)
            {
                AnalyzeText(context, text.TextToken.Text, text.TextToken.SpanStart, rawString);
            }
        }
    }

    private static void AnalyzeText(SyntaxNodeAnalysisContext context, string text, int start, MultiLineRawString? rawString)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is >= ' ' and <= '~')
                continue;

            var codePoint = UseEscapeSequenceForInvisibleCharacterCommon.GetCodePoint(text, i, out var length);
            if (UseEscapeSequenceForInvisibleCharacterCommon.IsInvisibleCharacter(codePoint) && (rawString is null || rawString.IsContent(start + i)))
            {
                var location = Location.Create(context.Node.SyntaxTree, new TextSpan(start + i, length));
                context.ReportDiagnostic(Rule, location, UseEscapeSequenceForInvisibleCharacterCommon.GetCodePointName(codePoint));
            }

            i += length - 1;
        }
    }

    // The whitespace after the opening delimiter, before the closing delimiter, and the indentation of the lines are not part of the value of a multi-line raw string
    private sealed class MultiLineRawString(TextLineCollection lines, int startLine, int endLine, int indentation)
    {
        public static MultiLineRawString Create(SyntaxNode node, CancellationToken cancellationToken)
        {
            var text = node.SyntaxTree.GetText(cancellationToken);
            var lastLine = text.Lines.GetLineFromPosition(node.Span.End);
            var indentation = 0;
            while (lastLine.Start + indentation < lastLine.End && text[lastLine.Start + indentation] != '"')
            {
                indentation++;
            }

            return new MultiLineRawString(text.Lines, text.Lines.GetLineFromPosition(node.SpanStart).LineNumber, lastLine.LineNumber, indentation);
        }

        public bool IsContent(int position)
        {
            var line = lines.GetLineFromPosition(position);
            return line.LineNumber != startLine && line.LineNumber != endLine && position - line.Start >= indentation;
        }
    }
}

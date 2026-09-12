using System.Text;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class DoNotUseInterpolatedStringWithoutParametersFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.DoNotUseInterpolatedStringWithoutParameters);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not InterpolatedStringExpressionSyntax interpolatedString)
            return;

        var regularString = await CreateRegularStringAsync(context.Document, interpolatedString, context.CancellationToken).ConfigureAwait(false);
        if (regularString is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Convert to regular string",
                ct => ConvertToRegularString(context.Document, interpolatedString, regularString, ct),
                equivalenceKey: "Convert to regular string"),
            context.Diagnostics);
    }

    private static async Task<ExpressionSyntax?> CreateRegularStringAsync(Document document, InterpolatedStringExpressionSyntax interpolatedString, CancellationToken cancellationToken)
    {
        // Check if this is a raw string literal (C# 11+)
        if (interpolatedString.StringStartToken.Kind() is SyntaxKind.InterpolatedMultiLineRawStringStartToken or SyntaxKind.InterpolatedSingleLineRawStringStartToken)
        {
            // For raw strings, remove the whole $ prefix from the start token, as the number of $ determines
            // the number of braces that start an interpolation: $$"""{text}""" -> """{text}"""
            var startTokenText = interpolatedString.StringStartToken.Text;
            var dollarCount = startTokenText.Length - startTokenText.TrimStart('$').Length;
            if (dollarCount is 0)
                return null;

            // The text of the node starts with the start token and does not include the trivia
            var newText = interpolatedString.ToString().Substring(dollarCount);
            var newNode = SyntaxFactory.ParseExpression(newText, options: interpolatedString.SyntaxTree.Options);
            if (!newNode.IsKind(SyntaxKind.StringLiteralExpression) || newNode.ContainsDiagnostics)
                return null;

            return newNode;
        }

        // The text of the tokens still contains the escaped braces ("{{" and "}}"),
        // so use the value computed by the compiler
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetOperation(interpolatedString, cancellationToken) is not IInterpolatedStringOperation operation)
            return null;

        var stringContent = new StringBuilder();
        foreach (var part in operation.Parts)
        {
            if (part is not IInterpolatedStringTextOperation { Text.ConstantValue: { HasValue: true, Value: string text } })
                return null;

            stringContent.Append(text);
        }

        return SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(stringContent.ToString()));
    }

    private static async Task<Document> ConvertToRegularString(Document document, InterpolatedStringExpressionSyntax interpolatedString, ExpressionSyntax regularString, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(interpolatedString, regularString.WithTriviaFrom(interpolatedString));
        return editor.GetChangedDocument();
    }
}

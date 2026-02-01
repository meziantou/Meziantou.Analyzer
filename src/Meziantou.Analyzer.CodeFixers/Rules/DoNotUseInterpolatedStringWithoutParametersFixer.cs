using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

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

        context.RegisterCodeFix(
            CodeAction.Create(
                "Convert to regular string",
                ct => ConvertToRegularString(context.Document, interpolatedString, ct),
                equivalenceKey: "Convert to regular string"),
            context.Diagnostics);
    }

    private static async Task<Document> ConvertToRegularString(Document document, InterpolatedStringExpressionSyntax interpolatedString, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // Extract the string content from the interpolated string
        var stringContent = string.Empty;
        foreach (var content in interpolatedString.Contents)
        {
            if (content is InterpolatedStringTextSyntax textSyntax)
            {
                // Use the ValueText which contains the actual string value (not escaped)
                stringContent += textSyntax.TextToken.ValueText;
            }
        }

        // Create a regular string literal with the same content
        var regularString = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(stringContent));

        editor.ReplaceNode(interpolatedString, regularString.WithTriviaFrom(interpolatedString));

        return editor.GetChangedDocument();
    }
}

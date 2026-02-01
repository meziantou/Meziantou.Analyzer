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

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not InterpolatedStringExpressionSyntax nodeToFix)
            return;

        var title = "Use string literal";
        var codeAction = CodeAction.Create(
            title,
            ct => Fix(context.Document, root, nodeToFix, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static Task<Document> Fix(Document document, SyntaxNode root, InterpolatedStringExpressionSyntax nodeToFix, CancellationToken _)
    {
        // Find the containing argument
        var argument = nodeToFix.Ancestors().OfType<ArgumentSyntax>().FirstOrDefault();
        if (argument is null)
            return Task.FromResult(document);

        var invocation = argument.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocation is null)
            return Task.FromResult(document);

        // Convert interpolated string to regular string literal
        var literalString = ConvertToStringLiteral(nodeToFix);

        // For string.Create, replace the entire invocation
        // For other methods (like StringBuilder.Append), just replace the argument
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;
            var typeName = memberAccess.Expression switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                PredefinedTypeSyntax predefined => predefined.Keyword.Text,
                _ => null,
            };

            // If it's string.Create, replace the entire invocation
            if (typeName == "string" && methodName == "Create")
            {
                var newRoot = root.ReplaceNode(invocation, literalString.WithTriviaFrom(invocation));
                return Task.FromResult(document.WithSyntaxRoot(newRoot));
            }
        }

        // For other cases, just replace the interpolated string with a regular string
        var newRoot2 = root.ReplaceNode(nodeToFix, literalString.WithTriviaFrom(nodeToFix));
        return Task.FromResult(document.WithSyntaxRoot(newRoot2));
    }

    private static LiteralExpressionSyntax ConvertToStringLiteral(InterpolatedStringExpressionSyntax interpolatedString)
    {
        // Extract the string content from the interpolated string parts
        var contents = string.Concat(interpolatedString.Contents.Select(c => c.ToString()));

        // Check the string format
        var stringStart = interpolatedString.StringStartToken.Text;
        var isRawString = stringStart.Contains("\"\"\"", StringComparison.Ordinal);
        var isVerbatim = stringStart.Contains('@', StringComparison.Ordinal);

        string literalText;
        string valueText;

        if (isRawString)
        {
            // Raw string literal - preserve the raw format but remove the $
            literalText = stringStart.Replace("$", "", StringComparison.Ordinal) + contents + interpolatedString.StringEndToken.Text;
            valueText = contents; // Raw strings have no escape sequences
        }
        else if (isVerbatim)
        {
            // Verbatim string - preserve the content as-is
            literalText = $"@\"{contents}\"";
            valueText = contents; // Verbatim strings preserve content literally (except "" for ")
        }
        else
        {
            // Regular string - need to preserve any escape sequences from the original
            literalText = $"\"{contents}\"";
            valueText = contents; // Interpolated strings already have parsed content
        }

        return SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Token(
                interpolatedString.StringStartToken.LeadingTrivia,
                SyntaxKind.StringLiteralToken,
                literalText,
                valueText,
                interpolatedString.StringEndToken.TrailingTrivia));
    }
}


using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Refactorings;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ConvertToStringFormatRefactoring))]
[Shared]
public sealed class ConvertToStringFormatRefactoring : CodeRefactoringProvider
{
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var node = root.FindNode(context.Span);
        if (node is not InterpolatedStringExpressionSyntax interpolatedString)
            return;

        var action = CodeAction.Create(
            "Convert to string.Format",
            cancellationToken => ConvertToStringFormat(context.Document, interpolatedString, cancellationToken),
            equivalenceKey: "ConvertToStringFormat");

        context.RegisterRefactoring(action);
    }

    private static async Task<Document> ConvertToStringFormat(Document document, InterpolatedStringExpressionSyntax interpolatedString, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var formatString = new StringBuilder();
        var arguments = new List<ArgumentSyntax>();
        var argumentIndex = 0;

        foreach (var content in interpolatedString.Contents)
        {
            if (content is InterpolatedStringTextSyntax textSyntax)
            {
                formatString.Append(textSyntax.TextToken.ValueText);
            }
            else if (content is InterpolationSyntax interpolation)
            {
                formatString.Append('{');
                formatString.Append(argumentIndex);

                if (interpolation.AlignmentClause is not null)
                {
                    formatString.Append(',');
                    formatString.Append(interpolation.AlignmentClause.Value.ToString());
                }

                if (interpolation.FormatClause is not null)
                {
                    formatString.Append(':');
                    formatString.Append(interpolation.FormatClause.FormatStringToken.ValueText);
                }

                formatString.Append('}');
                arguments.Add(SyntaxFactory.Argument(interpolation.Expression));
                argumentIndex++;
            }
        }

        var formatLiteral = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(formatString.ToString()));

        var argumentList = SyntaxFactory.SeparatedList<ArgumentSyntax>()
            .Add(SyntaxFactory.Argument(formatLiteral))
            .AddRange(arguments);

        var invocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                SyntaxFactory.IdentifierName("Format")),
            SyntaxFactory.ArgumentList(argumentList));

        editor.ReplaceNode(interpolatedString, invocation);
        return editor.GetChangedDocument();
    }
}

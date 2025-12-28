using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Refactorings;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MakeInterpolatedStringRefactoring))]
[Shared]
public sealed class MakeInterpolatedStringRefactoring : CodeRefactoringProvider
{
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var node = root.FindNode(context.Span);
        if (node is not LiteralExpressionSyntax literalExpression)
            return;

        if (IsInterpolatedString(literalExpression))
            return;

        if (IsRawString(literalExpression))
            return;

        var action = CodeAction.Create(
            "Convert to interpolated string",
            cancellationToken => ConvertToInterpolatedString(context.Document, literalExpression, cancellationToken),
            equivalenceKey: "ConvertToInterpolatedString");

        context.RegisterRefactoring(action);
    }

    private static async Task<Document> ConvertToInterpolatedString(Document document, LiteralExpressionSyntax literalExpression, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var newNode = SyntaxFactory.ParseExpression("$" + literalExpression.Token.Text);
        editor.ReplaceNode(literalExpression, newNode);
        return editor.GetChangedDocument();
    }

    private static bool IsRawString(LiteralExpressionSyntax node)
    {
        var token = node.Token.Text;
        return token.Contains("\"\"\"", StringComparison.Ordinal);
    }

    private static bool IsInterpolatedString(LiteralExpressionSyntax node)
    {
        var token = node.Token.Text;
        foreach (var c in token)
        {
            if (c == '"')
                return false;

            if (c == '$')
                return true;
        }

        return false;
    }
}

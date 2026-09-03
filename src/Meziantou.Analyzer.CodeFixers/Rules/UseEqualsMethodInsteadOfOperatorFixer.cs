using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseEqualsMethodInsteadOfOperatorFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseEqualsMethodInsteadOfOperator);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var binaryOp = FindBinaryOperation(semanticModel, nodeToFix, context.CancellationToken);
        if (binaryOp is null)
            return;

        if (binaryOp.Syntax is not BinaryExpressionSyntax)
            return;

        if (binaryOp.LeftOperand.Syntax is not ExpressionSyntax || binaryOp.RightOperand.Syntax is not ExpressionSyntax)
            return;

        const string Title = "Use Equals";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => UseEquals(context.Document, nodeToFix, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Document> UseEquals(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (FindBinaryOperation(editor.SemanticModel, nodeToFix, cancellationToken) is not { } operation)
            return document;

        if (operation.Syntax is not BinaryExpressionSyntax binaryExpression)
            return document;

        if (operation.LeftOperand.Syntax is not ExpressionSyntax leftOperand || operation.RightOperand.Syntax is not ExpressionSyntax rightOperand)
            return document;

        ExpressionSyntax newExpression = InvocationExpression(
            MemberAccessExpression(
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression,
                PredefinedType(Token(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ObjectKeyword)),
                IdentifierName(nameof(System.Object.Equals))),
            ArgumentList(SeparatedList(new[] { Argument(rightOperand.WithoutTrivia()) })));
        newExpression = ((InvocationExpressionSyntax)newExpression).WithArgumentList(
            ArgumentList(SeparatedList(new[] { Argument(leftOperand.WithoutTrivia()), Argument(rightOperand.WithoutTrivia()) })));

        if (operation.OperatorKind is BinaryOperatorKind.NotEquals)
        {
            newExpression = PrefixUnaryExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.LogicalNotExpression, newExpression.Parenthesize());
        }

        editor.ReplaceNode(binaryExpression, newExpression.WithTriviaFrom(binaryExpression).WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static IBinaryOperation? FindBinaryOperation(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
    {
        foreach (var candidate in node.AncestorsAndSelf())
        {
            if (semanticModel.GetOperation(candidate, cancellationToken) is IBinaryOperation binaryOperation)
                return binaryOperation;
        }

        return null;
    }
}

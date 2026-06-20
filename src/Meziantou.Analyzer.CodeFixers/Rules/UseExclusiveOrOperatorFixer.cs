using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseExclusiveOrOperatorFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseExclusiveOrOperator);

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

        if (!TryGetExclusiveOrOperation(semanticModel, nodeToFix, context.CancellationToken, out _))
            return;

        const string Title = "Use ^ operator";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => UseExclusiveOrOperator(context.Document, nodeToFix, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Document> UseExclusiveOrOperator(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (!TryGetExclusiveOrOperation(editor.SemanticModel, nodeToFix, cancellationToken, out var operation))
            return document;

        if (operation.Syntax is not BinaryExpressionSyntax binaryExpression)
            return document;

        if (!UseExclusiveOrOperatorCommon.TryMatch(operation, out var leftOperand, out var rightOperand))
            return document;

        if (leftOperand.Syntax is not ExpressionSyntax leftExpression || rightOperand.Syntax is not ExpressionSyntax rightExpression)
            return document;

        var newExpression = BinaryExpression(
            SyntaxKind.ExclusiveOrExpression,
            leftExpression.WithoutTrivia(),
            Token(SyntaxKind.CaretToken),
            rightExpression.WithoutTrivia());

        editor.ReplaceNode(binaryExpression, newExpression.WithTriviaFrom(binaryExpression).WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static bool TryGetExclusiveOrOperation(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken, out IBinaryOperation operation)
    {
        foreach (var candidate in node.AncestorsAndSelf())
        {
            if (semanticModel.GetOperation(candidate, cancellationToken) is IBinaryOperation binaryOperation &&
                UseExclusiveOrOperatorCommon.TryMatch(binaryOperation, out _, out _))
            {
                operation = binaryOperation;
                return true;
            }
        }

        operation = null!;
        return false;
    }
}

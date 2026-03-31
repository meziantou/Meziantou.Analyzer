using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseTaskUnwrapFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseTaskUnwrap);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        const string Title = "Use Unwrap";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => UseUnwrap(context.Document, nodeToFix, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Document> UseUnwrap(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (FindAwait(editor.SemanticModel, nodeToFix, cancellationToken) is not { } awaitOperation)
            return document;

        if (awaitOperation.Syntax is not AwaitExpressionSyntax awaitExpression)
            return document;

        if (awaitOperation.Operation is IAwaitOperation innerAwait)
        {
            var unwrappedExpression = InvocationExpression(
                MemberAccessExpression(
                    Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression,
                    WrapForMemberAccess((ExpressionSyntax)innerAwait.Operation.Syntax.WithoutTrivia()),
                    IdentifierName("Unwrap")));

            var newNode = awaitExpression.WithExpression(unwrappedExpression.WithTriviaFrom(awaitExpression.Expression));
            editor.ReplaceNode(awaitExpression, newNode.WithAdditionalAnnotations(Formatter.Annotation));
            return editor.GetChangedDocument();
        }

        if (awaitOperation.Operation is IInvocationOperation { Instance: IAwaitOperation innerAwaitOperation } &&
            awaitExpression.Expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var unwrappedExpression = InvocationExpression(
                MemberAccessExpression(
                    Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression,
                    WrapForMemberAccess((ExpressionSyntax)innerAwaitOperation.Operation.Syntax.WithoutTrivia()),
                    IdentifierName("Unwrap")));

            var newExpression = invocation.WithExpression(memberAccess.WithExpression(unwrappedExpression.WithTriviaFrom(memberAccess.Expression)));
            editor.ReplaceNode(awaitExpression, awaitExpression.WithExpression(newExpression).WithAdditionalAnnotations(Formatter.Annotation));
            return editor.GetChangedDocument();
        }

        return document;
    }

    private static IAwaitOperation? FindAwait(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
    {
        foreach (var candidate in node.AncestorsAndSelf())
        {
            if (semanticModel.GetOperation(candidate, cancellationToken) is IAwaitOperation awaitOperation)
                return awaitOperation;
        }

        return null;
    }

    private static ExpressionSyntax WrapForMemberAccess(ExpressionSyntax expression)
        => expression switch
        {
            IdentifierNameSyntax or
            GenericNameSyntax or
            ThisExpressionSyntax or
            BaseExpressionSyntax or
            InvocationExpressionSyntax or
            MemberAccessExpressionSyntax or
            ElementAccessExpressionSyntax => expression,
            _ => ParenthesizedExpression(expression),
        };
}


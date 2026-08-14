using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
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
public sealed class ReturnTaskInsteadOfAwaitingItFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.ReturnTaskInsteadOfAwaitingIt);

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

        var awaitOperation = FindAwait(semanticModel, nodeToFix, context.CancellationToken);
        if (awaitOperation?.Syntax is not AwaitExpressionSyntax awaitExpression)
            return;

        if (awaitExpression.FirstAncestorOrSelf<SyntaxNode>(IsFunction) is null)
            return;

        const string Title = "Return the task directly";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => FixAsync(context.Document, nodeToFix, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Document> FixAsync(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var awaitOperation = FindAwait(editor.SemanticModel, nodeToFix, cancellationToken);
        if (awaitOperation?.Syntax is not AwaitExpressionSyntax awaitExpression)
            return document;

        var function = awaitExpression.FirstAncestorOrSelf<SyntaxNode>(IsFunction);
        if (function is null)
            return document;

        // Recover the underlying task, ignoring a trailing ConfigureAwait call
        var innerExpression = awaitExpression.Expression;
        if (awaitOperation.Operation is IInvocationOperation { Instance: { } instance, TargetMethod.Name: "ConfigureAwait" } &&
            instance.Syntax is ExpressionSyntax instanceExpression)
        {
            innerExpression = instanceExpression;
        }

        SyntaxNode newFunction;
        if (awaitExpression.Parent is ExpressionStatementSyntax expressionStatement)
        {
            // "await X;" => "return X;"
            var returnStatement = ReturnStatement(innerExpression.WithoutTrivia()).WithTriviaFrom(expressionStatement);
            newFunction = function.ReplaceNode(expressionStatement, returnStatement);
        }
        else
        {
            newFunction = function.ReplaceNode(awaitExpression, innerExpression.WithTriviaFrom(awaitExpression));
        }

        newFunction = RemoveAsyncModifier(newFunction, editor.Generator);
        editor.ReplaceNode(function, newFunction.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static SyntaxNode RemoveAsyncModifier(SyntaxNode function, SyntaxGenerator generator)
    {
        switch (function)
        {
            case MethodDeclarationSyntax:
            case LocalFunctionStatementSyntax:
                return generator.WithModifiers(function, generator.GetModifiers(function).WithAsync(isAsync: false));

            case ParenthesizedLambdaExpressionSyntax lambda:
                return lambda.WithAsyncKeyword(default).WithLeadingTrivia(lambda.AsyncKeyword.LeadingTrivia);

            case SimpleLambdaExpressionSyntax lambda:
                return lambda.WithAsyncKeyword(default).WithLeadingTrivia(lambda.AsyncKeyword.LeadingTrivia);

            case AnonymousMethodExpressionSyntax anonymousMethod:
                return anonymousMethod.WithAsyncKeyword(default).WithLeadingTrivia(anonymousMethod.AsyncKeyword.LeadingTrivia);

            default:
                return function;
        }
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

    private static bool IsFunction(SyntaxNode node)
    {
        return node is MethodDeclarationSyntax or LocalFunctionStatementSyntax or ParenthesizedLambdaExpressionSyntax or SimpleLambdaExpressionSyntax or AnonymousMethodExpressionSyntax;
    }
}

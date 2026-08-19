using Microsoft.CodeAnalysis.Formatting;
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
        if (nodeToFix?.FirstAncestorOrSelf<SyntaxNode>(IsFunction) is null)
            return;

        const string Title = "Return the task directly";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => FixAsync(context.Document, nodeToFix, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Document> FixAsync(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;

        var function = nodeToFix.FirstAncestorOrSelf<SyntaxNode>(IsFunction);
        if (function is null)
            return document;

        // Every await in the function (excluding nested functions) can be removed
        var awaitExpressions = function
            .DescendantNodesAndSelf(descendIntoChildren: node => node == function || !IsFunction(node))
            .OfType<AwaitExpressionSyntax>()
            .ToList();

        var replacements = new Dictionary<SyntaxNode, SyntaxNode>();
        foreach (var awaitExpression in awaitExpressions)
        {
            var innerExpression = awaitExpression.Expression;
            if (semanticModel.GetOperation(awaitExpression, cancellationToken) is IAwaitOperation { Operation: IInvocationOperation { Instance: { } instance, TargetMethod.Name: "ConfigureAwait" } } &&
                instance.Syntax is ExpressionSyntax instanceExpression)
            {
                innerExpression = instanceExpression;
            }

            if (awaitExpression.Parent is ExpressionStatementSyntax expressionStatement)
            {
                // "await X;" => "return X;"
                replacements[expressionStatement] = ReturnStatement(innerExpression.WithoutTrivia()).WithTriviaFrom(expressionStatement);
            }
            else
            {
                replacements[awaitExpression] = innerExpression.WithTriviaFrom(awaitExpression);
            }
        }

        if (replacements.Count == 0)
            return document;

        var newFunction = function.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]);
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

    private static bool IsFunction(SyntaxNode node)
    {
        return node is MethodDeclarationSyntax or LocalFunctionStatementSyntax or ParenthesizedLambdaExpressionSyntax or SimpleLambdaExpressionSyntax or AnonymousMethodExpressionSyntax;
    }
}

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
        var function = root?.FindNode(context.Span, getInnermostNodeForTie: true).FirstAncestorOrSelf<SyntaxNode>(IsFunction);
        if (function is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var replacements = GetReplacements(semanticModel, function, context.CancellationToken);
        if (replacements is null)
            return;

        const string Title = "Return the task directly";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => FixAsync(context.Document, function, replacements, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    // Computes how every await of the function (excluding the nested functions) is rewritten,
    // or null when one of them cannot be removed
    private static Dictionary<SyntaxNode, SyntaxNode>? GetReplacements(SemanticModel semanticModel, SyntaxNode function, CancellationToken cancellationToken)
    {
        var configureAwaitOptionsSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ConfigureAwaitOptions");
        var awaitExpressions = function
            .DescendantNodesAndSelf(descendIntoChildren: node => node == function || !IsFunction(node))
            .OfType<AwaitExpressionSyntax>();

        var replacements = new Dictionary<SyntaxNode, SyntaxNode>();
        foreach (var awaitExpression in awaitExpressions)
        {
            var innerExpression = awaitExpression.Expression;
            if (semanticModel.GetOperation(awaitExpression, cancellationToken) is IAwaitOperation { Operation: IInvocationOperation { Instance: { } instance, TargetMethod.Name: "ConfigureAwait" } invocation } &&
                instance.Syntax is ExpressionSyntax instanceExpression)
            {
                if (!ReturnTaskInsteadOfAwaitingItCommon.CanRemoveConfigureAwait(invocation, configureAwaitOptionsSymbol))
                    return null;

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

        return replacements.Count > 0 ? replacements : null;
    }

    private static async Task<Document> FixAsync(Document document, SyntaxNode function, Dictionary<SyntaxNode, SyntaxNode> replacements, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

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

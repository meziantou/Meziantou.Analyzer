using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
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

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var awaitOp = FindAwait(semanticModel, nodeToFix, context.CancellationToken);
        if (awaitOp is null)
            return;

        if (awaitOp.Syntax is not AwaitExpressionSyntax)
            return;

        // Unwrap is an extension method, so its namespace must be imported for the new invocation to compile
        var taskExtensionsSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.TaskExtensions");
        if (taskExtensionsSymbol is null)
            return;

        const string Title = "Use Unwrap";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => UseUnwrap(context.Document, nodeToFix, taskExtensionsSymbol, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Document> UseUnwrap(Document document, SyntaxNode nodeToFix, INamedTypeSymbol taskExtensionsSymbol, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (FindAwait(editor.SemanticModel, nodeToFix, cancellationToken) is not { } awaitOperation)
            return document;

        if (awaitOperation.Syntax is not AwaitExpressionSyntax awaitExpression)
            return document;

        if (awaitOperation.Operation is IAwaitOperation innerAwait)
        {
            var unwrappedExpression = CreateUnwrapInvocation(editor.Generator, innerAwait.Operation.Syntax, taskExtensionsSymbol);

            var newNode = awaitExpression.WithExpression(unwrappedExpression.WithTriviaFrom(awaitExpression.Expression));
            editor.ReplaceNode(awaitExpression, newNode.WithAdditionalAnnotations(Formatter.Annotation));
            return editor.GetChangedDocument();
        }

        if (awaitOperation.Operation is IInvocationOperation { Instance: IAwaitOperation innerAwaitOperation } &&
            awaitExpression.Expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var unwrappedExpression = CreateUnwrapInvocation(editor.Generator, innerAwaitOperation.Operation.Syntax, taskExtensionsSymbol);

            var newExpression = invocation.WithExpression(memberAccess.WithExpression(unwrappedExpression.WithTriviaFrom(memberAccess.Expression)));
            editor.ReplaceNode(awaitExpression, awaitExpression.WithExpression(newExpression).WithAdditionalAnnotations(Formatter.Annotation));
            return editor.GetChangedDocument();
        }

        return document;
    }

    private static InvocationExpressionSyntax CreateUnwrapInvocation(SyntaxGenerator generator, SyntaxNode task, INamedTypeSymbol taskExtensionsSymbol)
    {
        // The type expression is annotated with the symbol of TaskExtensions. Copying its annotations lets the code action
        // add the using directive of the extension method when it is not in scope.
        var unwrapName = generator.TypeExpression(taskExtensionsSymbol, addImport: true)
            .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation)
            .CopyAnnotationsTo(IdentifierName("Unwrap"));

        return InvocationExpression(
            MemberAccessExpression(
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression,
                (ExpressionSyntax)task.WithoutTrivia().Parenthesize(),
                unwrapName));
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
}


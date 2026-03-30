using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
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
public sealed class UseLazyInitializerEnsureInitializeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(RuleIdentifiers.UseLazyInitializerEnsureInitialize);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use LazyInitializer.EnsureInitialized",
                ct => Update(context.Document, nodeToFix, ct),
                equivalenceKey: "Use LazyInitializer.EnsureInitialized"),
            context.Diagnostics);
    }

    private static async Task<Document> Update(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;

        if (semanticModel.GetOperation(nodeToFix, cancellationToken) is not IInvocationOperation invocation)
            return document;

        var lazyInitializerType = semanticModel.Compilation.GetBestTypeByMetadataName("System.Threading.LazyInitializer");
        if (lazyInitializerType is null)
            return document;

        var generator = editor.Generator;
        var refArgSyntax = (ArgumentSyntax)invocation.Arguments[0].Syntax;
        var valueExpression = ((ArgumentSyntax)invocation.Arguments[1].Syntax).Expression;

        var lambdaExpression = ParenthesizedLambdaExpression()
            .WithExpressionBody(valueExpression.WithoutTrivia());

        var newInvocation = InvocationExpression(
            (ExpressionSyntax)generator.MemberAccessExpression(
                generator.TypeExpression(lazyInitializerType),
                "EnsureInitialized"))
            .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(
            [
                refArgSyntax,
                Argument(lambdaExpression),
            ])));

        editor.ReplaceNode(nodeToFix, newInvocation
            .WithTriviaFrom(nodeToFix)
            .WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }
}

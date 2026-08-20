using Microsoft.CodeAnalysis.Formatting;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseStaticLambdaFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseStaticLambda);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not AnonymousFunctionExpressionSyntax anonymousFunction)
            return;

        if (anonymousFunction.Modifiers.Any(SyntaxKind.StaticKeyword))
            return;

        var title = "Add static modifier";
        var codeAction = CodeAction.Create(
            title,
            ct => AddStaticModifier(context.Document, anonymousFunction, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> AddStaticModifier(Document document, AnonymousFunctionExpressionSyntax anonymousFunction, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var staticToken = SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space);

        // The leading trivia must be moved to the new first token of the lambda
        var leadingTrivia = anonymousFunction.GetLeadingTrivia();
        var newNode = anonymousFunction
            .WithoutLeadingTrivia()
            .WithModifiers(anonymousFunction.Modifiers.Insert(0, staticToken))
            .WithLeadingTrivia(leadingTrivia)
            .WithAdditionalAnnotations(Formatter.Annotation);

        editor.ReplaceNode(anonymousFunction, newNode);
        return editor.GetChangedDocument();
    }
}

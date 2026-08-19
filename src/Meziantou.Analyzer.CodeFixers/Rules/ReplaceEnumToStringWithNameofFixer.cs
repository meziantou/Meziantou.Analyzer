namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class ReplaceEnumToStringWithNameofFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.ReplaceEnumToStringWithNameof);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (semanticModel.GetOperation(nodeToFix, context.CancellationToken) is null)
            return;

        var title = "Use nameof";
        context.RegisterCodeFix(CodeAction.Create(title, ct => UseNameof(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
    }

    private static async Task<Document> UseNameof(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var operation = editor.SemanticModel.GetOperation(nodeToFix, cancellationToken);
        if (operation is IInvocationOperation invocation && invocation.Instance is not null)
        {
            var newExpression = generator.NameOfExpression(invocation.Instance.Syntax);
            editor.ReplaceNode(nodeToFix, newExpression);
        }
        else if (operation is IInterpolationOperation interpolation)
        {
            var newExpression = SyntaxFactory.Interpolation((ExpressionSyntax)generator.NameOfExpression(interpolation.Expression.Syntax));
            editor.ReplaceNode(nodeToFix, newExpression);
        }

        return editor.GetChangedDocument();
    }
}

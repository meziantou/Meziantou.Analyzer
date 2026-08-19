namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class ThrowIfNullWithNonNullableInstanceFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.ThrowIfNullWithNonNullableInstance);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var invocation = nodeToFix as InvocationExpressionSyntax ?? nodeToFix.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocation is null)
            return;

        if (invocation.Parent is not ExpressionStatementSyntax)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Remove useless ThrowIfNull",
                ct => RemoveInvocation(context.Document, invocation, ct),
                equivalenceKey: "Remove useless ThrowIfNull"),
            context.Diagnostics);
    }

    private static async Task<Document> RemoveInvocation(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.RemoveNode(invocation.Parent!);
        return editor.GetChangedDocument();
    }
}

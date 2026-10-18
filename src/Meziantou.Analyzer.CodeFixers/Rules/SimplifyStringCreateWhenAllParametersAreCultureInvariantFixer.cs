namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class SimplifyStringCreateWhenAllParametersAreCultureInvariantFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.SimplifyStringCreateWhenAllParametersAreCultureInvariant);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not InvocationExpressionSyntax nodeToFix)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetOperation(nodeToFix, context.CancellationToken) is not IInvocationOperation { Arguments: [_, var interpolatedStringArgument] } operation)
            return;

        // An interpolated string cannot be used as a statement: string.Create(CultureInfo.InvariantCulture, $"");
        if (operation.Parent is IExpressionStatementOperation)
            return;

        // The Content property contains the interpolated string operation
        if (interpolatedStringArgument.Value is not IInterpolatedStringHandlerCreationOperation { Content: IInterpolatedStringOperation { Syntax: InterpolatedStringExpressionSyntax interpolatedStringSyntax } })
            return;

        var title = "Simplify to interpolated string";
        var codeAction = CodeAction.Create(
            title,
            ct => Fix(context.Document, nodeToFix, interpolatedStringSyntax, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> Fix(Document document, InvocationExpressionSyntax nodeToFix, InterpolatedStringExpressionSyntax interpolatedStringSyntax, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(nodeToFix, interpolatedStringSyntax);
        return editor.GetChangedDocument();
    }
}

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseAnOverloadThatHasTimeProviderFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseAnOverloadThatHasTimeProviderWhenAvailable);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not InvocationExpressionSyntax invocationExpression)
            return;

        if (!int.TryParse(context.Diagnostics[0].Properties.GetValueOrDefault(UseAnOverloadThatHasTimeProviderAnalyzerCommon.ParameterIndexKey), NumberStyles.None, CultureInfo.InvariantCulture, out var parameterIndex))
            return;

        if (!context.Diagnostics[0].Properties.TryGetValue(UseAnOverloadThatHasTimeProviderAnalyzerCommon.ParameterNameKey, out var parameterName) || parameterName is null)
            return;

        if (!context.Diagnostics[0].Properties.TryGetValue(UseAnOverloadThatHasTimeProviderAnalyzerCommon.PathsKey, out var paths) || paths is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var timeProviderSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.TimeProvider");
        if (timeProviderSymbol is null)
            return;

        var generator = SyntaxGenerator.GetGenerator(context.Document);
        foreach (var path in paths.Split(','))
        {
            var newInvocation = ArgumentListHelper.AddArgument(
                semanticModel,
                generator,
                invocationExpression,
                parameterIndex,
                parameterName,
                SyntaxFactory.ParseExpression(path),
                parameter => parameter.Type.IsEqualTo(timeProviderSymbol),
                cancellationToken: context.CancellationToken);

            if (newInvocation is null)
                continue;

            var title = "Use TimeProvider:  " + path;
            var codeAction = CodeAction.Create(
                title,
                ct => FixInvocation(context.Document, invocationExpression, newInvocation, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static async Task<Document> FixInvocation(Document document, InvocationExpressionSyntax nodeToFix, InvocationExpressionSyntax newInvocation, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(nodeToFix, newInvocation);
        return editor.GetChangedDocument();
    }
}

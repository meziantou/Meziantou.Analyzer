namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseAnOverloadThatHasCancellationTokenFixer_Argument : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailable);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not InvocationExpressionSyntax invocationExpression)
            return;

        if (!int.TryParse(context.Diagnostics[0].Properties.GetValueOrDefault(UseAnOverloadThatHasCancellationTokenAnalyzerCommon.ParameterIndexKey), NumberStyles.None, CultureInfo.InvariantCulture, out var parameterIndex))
            return;

        if (!context.Diagnostics[0].Properties.TryGetValue(UseAnOverloadThatHasCancellationTokenAnalyzerCommon.ParameterNameKey, out var parameterName) || parameterName is null)
            return;

        if (!context.Diagnostics[0].Properties.TryGetValue(UseAnOverloadThatHasCancellationTokenAnalyzerCommon.ParameterIsEnumeratorCancellationKey, out var parameterIsEnumeratorCancellation) || !bool.TryParse(parameterIsEnumeratorCancellation, out var isEnumeratorCancellation))
            return;

        if (!context.Diagnostics[0].Properties.TryGetValue(UseAnOverloadThatHasCancellationTokenAnalyzerCommon.CancellationTokensKey, out var cancellationTokens) || cancellationTokens is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var cancellationTokenSymbol = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        if (cancellationTokenSymbol is null)
            return;

        var namespaceToImport = context.Diagnostics[0].Properties.GetValueOrDefault(OverloadFinder.NamespaceToImportPropertyName);
        var generator = SyntaxGenerator.GetGenerator(context.Document);
        foreach (var cancellationToken in cancellationTokens.Split(','))
        {
            var cancellationTokenExpression = SyntaxFactory.ParseExpression(cancellationToken);
            var newInvocation = ArgumentListHelper.AddArgument(
                semanticModel,
                generator,
                invocationExpression,
                parameterIndex,
                parameterName,
                cancellationTokenExpression,
                parameter => parameter.Type.IsEqualTo(cancellationTokenSymbol),
                namespaceToImport: namespaceToImport,
                cancellationToken: context.CancellationToken);

            if (newInvocation is null)
                continue;

            var nodeToReplace = isEnumeratorCancellation
                ? GetRedundantWithCancellation(semanticModel, invocationExpression, cancellationTokenExpression, context.CancellationToken) ?? invocationExpression
                : invocationExpression;

            var title = "Use CancellationToken:  " + cancellationToken;
            var codeAction = CodeAction.Create(
                title,
                ct => FixInvocation(context.Document, nodeToReplace, newInvocation, namespaceToImport, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    /// <summary>
    /// Gets the enclosing <c>WithCancellation</c> invocation that becomes redundant once the token is passed to the method itself.
    /// </summary>
    private static SyntaxNode? GetRedundantWithCancellation(SemanticModel semanticModel, InvocationExpressionSyntax nodeToFix, ExpressionSyntax cancellationTokenExpression, CancellationToken cancellationToken)
    {
        if (semanticModel.GetOperation(nodeToFix, cancellationToken) is not IInvocationOperation invocation)
            return null;

        if (invocation.Parent?.Parent is not IInvocationOperation { TargetMethod.Name: "WithCancellation", Arguments: [{ }, { Value: var withCancellationArgument }] } parent)
            return null;

        if (!withCancellationArgument.Syntax.IsEquivalentTo(cancellationTokenExpression))
            return null;

        return parent.Syntax;
    }

    private static async Task<Document> FixInvocation(Document document, SyntaxNode nodeToReplace, InvocationExpressionSyntax newInvocation, string? namespaceToImport, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(nodeToReplace, newInvocation);
        if (namespaceToImport is not null)
        {
            UsingDirectiveHelper.AddUsingDirective(editor, nodeToReplace, namespaceToImport);
        }

        return editor.GetChangedDocument();
    }
}

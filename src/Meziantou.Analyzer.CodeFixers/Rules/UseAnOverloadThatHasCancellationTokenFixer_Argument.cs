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

        var cancellationTokenSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.Threading.CancellationToken");
        if (cancellationTokenSymbol is null)
            return;

        var generator = SyntaxGenerator.GetGenerator(context.Document);
        foreach (var cancellationToken in cancellationTokens.Split(','))
        {
            var cancellationTokenExpression = SyntaxFactory.ParseExpression(cancellationToken);
            var newInvocation = CreateInvocation(semanticModel, generator, invocationExpression, parameterIndex, parameterName, cancellationTokenExpression, cancellationTokenSymbol);
            if (newInvocation is null)
                continue;

            var nodeToReplace = isEnumeratorCancellation
                ? GetRedundantWithCancellation(semanticModel, invocationExpression, cancellationTokenExpression, context.CancellationToken) ?? invocationExpression
                : invocationExpression;

            var title = "Use CancellationToken:  " + cancellationToken;
            var codeAction = CodeAction.Create(
                title,
                ct => FixInvocation(context.Document, nodeToReplace, newInvocation, ct),
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

    private static InvocationExpressionSyntax? CreateInvocation(SemanticModel semanticModel, SyntaxGenerator generator, InvocationExpressionSyntax nodeToFix, int parameterIndex, string parameterName, ExpressionSyntax cancellationTokenExpression, INamedTypeSymbol cancellationTokenSymbol)
    {
        var arguments = nodeToFix.ArgumentList.Arguments;

        // A positional argument can only be added at the index of the parameter when all the arguments written before it are positional.
        // Otherwise, C# does not allow it (CS1738, CS1739, CS8323) or it would be bound to the wrong parameter.
        if (parameterIndex <= arguments.Count && !arguments.Take(parameterIndex).Any(argument => argument.NameColon is not null))
        {
            var positionalArgument = (ArgumentSyntax)generator.Argument(cancellationTokenExpression);
            var candidate = ReplaceArguments(nodeToFix, arguments.Insert(parameterIndex, positionalArgument));
            if (GetTargetMethod(semanticModel, nodeToFix, candidate) is { } method && parameterIndex < method.Parameters.Length && method.Parameters[parameterIndex].Type.IsEqualTo(cancellationTokenSymbol))
                return candidate;
        }

        // A named argument added at the end of the list is valid whatever the order of the existing arguments
        var namedArgument = (ArgumentSyntax)generator.Argument(parameterName, RefKind.None, cancellationTokenExpression);
        var namedCandidate = ReplaceArguments(nodeToFix, arguments.Add(namedArgument));
        var namedParameter = GetTargetMethod(semanticModel, nodeToFix, namedCandidate)?.Parameters.FirstOrDefault(parameter => string.Equals(parameter.Name, parameterName, StringComparison.Ordinal));
        if (namedParameter is not null && namedParameter.Type.IsEqualTo(cancellationTokenSymbol))
            return namedCandidate;

        return null;
    }

    private static InvocationExpressionSyntax ReplaceArguments(InvocationExpressionSyntax nodeToFix, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        return nodeToFix.WithArgumentList(nodeToFix.ArgumentList.WithArguments(arguments));
    }

    /// <summary>
    /// Gets the method the invocation would be bound to, so the fix is only offered when the new invocation compiles
    /// and the new argument is bound to the expected parameter.
    /// </summary>
    private static IMethodSymbol? GetTargetMethod(SemanticModel semanticModel, InvocationExpressionSyntax nodeToFix, InvocationExpressionSyntax newInvocation)
    {
        return semanticModel.GetSpeculativeSymbolInfo(nodeToFix.SpanStart, newInvocation, SpeculativeBindingOption.BindAsExpression).Symbol as IMethodSymbol;
    }

    private static async Task<Document> FixInvocation(Document document, SyntaxNode nodeToReplace, InvocationExpressionSyntax newInvocation, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(nodeToReplace, newInvocation);
        return editor.GetChangedDocument();
    }
}

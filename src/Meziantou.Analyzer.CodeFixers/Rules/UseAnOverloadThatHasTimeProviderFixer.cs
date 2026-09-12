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
            var newInvocation = CreateInvocation(semanticModel, generator, invocationExpression, parameterIndex, parameterName, path, timeProviderSymbol);
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

    private static InvocationExpressionSyntax? CreateInvocation(SemanticModel semanticModel, SyntaxGenerator generator, InvocationExpressionSyntax nodeToFix, int parameterIndex, string parameterName, string timeProviderPath, INamedTypeSymbol timeProviderSymbol)
    {
        var timeProviderExpression = SyntaxFactory.ParseExpression(timeProviderPath);
        var arguments = nodeToFix.ArgumentList.Arguments;

        // A positional argument can only be added at the index of the parameter when all the arguments written before it are positional.
        // Otherwise, C# does not allow it (CS1738, CS1739, CS8323) or it would be bound to the wrong parameter.
        if (parameterIndex <= arguments.Count && !arguments.Take(parameterIndex).Any(argument => argument.NameColon is not null))
        {
            var positionalArgument = (ArgumentSyntax)generator.Argument(timeProviderExpression);
            var candidate = ReplaceArguments(nodeToFix, arguments.Insert(parameterIndex, positionalArgument));
            if (GetTargetMethod(semanticModel, nodeToFix, candidate) is { } method && parameterIndex < method.Parameters.Length && method.Parameters[parameterIndex].Type.IsEqualTo(timeProviderSymbol))
                return candidate;
        }

        // A named argument added at the end of the list is valid whatever the order of the existing arguments
        var namedArgument = (ArgumentSyntax)generator.Argument(parameterName, RefKind.None, timeProviderExpression);
        var namedCandidate = ReplaceArguments(nodeToFix, arguments.Add(namedArgument));
        var namedParameter = GetTargetMethod(semanticModel, nodeToFix, namedCandidate)?.Parameters.FirstOrDefault(parameter => string.Equals(parameter.Name, parameterName, StringComparison.Ordinal));
        if (namedParameter is not null && namedParameter.Type.IsEqualTo(timeProviderSymbol))
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

    private static async Task<Document> FixInvocation(Document document, InvocationExpressionSyntax nodeToFix, InvocationExpressionSyntax newInvocation, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(nodeToFix, newInvocation);
        return editor.GetChangedDocument();
    }
}

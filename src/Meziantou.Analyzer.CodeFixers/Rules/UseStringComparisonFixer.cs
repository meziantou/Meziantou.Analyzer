namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseStringComparisonFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseStringComparison, RuleIdentifiers.AvoidCultureSensitiveMethod);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        // In case the ArrayCreationExpressionSyntax is wrapped in an ArgumentSyntax or some other node with the same span,
        // get the innermost node for ties.
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not InvocationExpressionSyntax invocationExpression)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var stringComparisonSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.StringComparison");
        if (stringComparisonSymbol is null)
            return;

        var parameter = FindStringComparisonParameter(semanticModel, invocationExpression, stringComparisonSymbol, context.CancellationToken);
        if (parameter is null)
            return;

        var generator = SyntaxGenerator.GetGenerator(context.Document);
        AddCodeFix(nameof(StringComparison.Ordinal));
        AddCodeFix(nameof(StringComparison.OrdinalIgnoreCase));

        void AddCodeFix(string comparisonMode)
        {
            var newInvocation = CreateInvocation(semanticModel, generator, invocationExpression, parameter, stringComparisonSymbol, comparisonMode);
            if (newInvocation is null)
                return;

            var title = "Add StringComparison." + comparisonMode;
            var codeAction = CodeAction.Create(
                title,
                ct => FixInvocation(context.Document, invocationExpression, newInvocation, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static InvocationExpressionSyntax? CreateInvocation(SemanticModel semanticModel, SyntaxGenerator generator, InvocationExpressionSyntax nodeToFix, IParameterSymbol parameter, INamedTypeSymbol stringComparison, string stringComparisonMode)
    {
        var comparisonExpression = generator.TypeMemberAccessExpression(stringComparison, stringComparisonMode, addImport: true);
        var arguments = nodeToFix.ArgumentList.Arguments;
        var parameterIndex = parameter.Ordinal;

        // A positional argument can only be added at the index of the parameter when all the arguments written before it are positional.
        // Otherwise, C# does not allow it (CS1738, CS1739, CS8323) or it would be bound to the wrong parameter.
        if (parameterIndex <= arguments.Count && !arguments.Take(parameterIndex).Any(argument => argument.NameColon is not null))
        {
            var positionalArgument = (ArgumentSyntax)generator.Argument(comparisonExpression);
            var candidate = ReplaceArguments(nodeToFix, arguments.Insert(parameterIndex, positionalArgument));
            if (GetTargetMethod(semanticModel, nodeToFix, candidate) is { } method && parameterIndex < method.Parameters.Length && method.Parameters[parameterIndex].Type.IsEqualTo(stringComparison))
                return candidate;
        }

        // A named argument added at the end of the list is valid whatever the order of the existing arguments
        var namedArgument = (ArgumentSyntax)generator.Argument(parameter.Name, RefKind.None, comparisonExpression);
        var namedCandidate = ReplaceArguments(nodeToFix, arguments.Add(namedArgument));
        var namedParameter = GetTargetMethod(semanticModel, nodeToFix, namedCandidate)?.Parameters.FirstOrDefault(candidate => string.Equals(candidate.Name, parameter.Name, StringComparison.Ordinal));
        if (namedParameter is not null && namedParameter.Type.IsEqualTo(stringComparison))
            return namedCandidate;

        return null;
    }

    private static InvocationExpressionSyntax ReplaceArguments(InvocationExpressionSyntax nodeToFix, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        return nodeToFix.WithArgumentList(nodeToFix.ArgumentList.WithArguments(arguments));
    }

    /// <summary>
    /// Gets the method the invocation would be bound to, so the fix is only offered when the new invocation compiles
    /// and the new argument is bound to the StringComparison parameter.
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

    private static IParameterSymbol? FindStringComparisonParameter(SemanticModel semanticModel, SyntaxNode node, INamedTypeSymbol stringComparison, CancellationToken cancellationToken)
    {
        if (semanticModel.GetOperation(node, cancellationToken) is not IInvocationOperation operation)
            return null;

        var overloadFinder = new OverloadFinder(semanticModel.Compilation);
        var overload = overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, options: default, [stringComparison]);
        if (overload is null)
            return null;

        return overload.Parameters.FirstOrDefault(parameter => parameter.Type.IsEqualTo(stringComparison));
    }
}

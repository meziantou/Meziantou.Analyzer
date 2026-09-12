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
            var newInvocation = ArgumentListHelper.AddArgument(
                semanticModel,
                generator,
                invocationExpression,
                parameter.Ordinal,
                parameter.Name,
                generator.TypeMemberAccessExpression(stringComparisonSymbol, comparisonMode, addImport: true),
                candidate => candidate.Type.IsEqualTo(stringComparisonSymbol),
                cancellationToken: context.CancellationToken);

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

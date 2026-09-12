namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseAnOverloadThatHasMidpointRoundingFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseAnOverloadThatHasMidpointRounding);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var invocationExpression = nodeToFix as InvocationExpressionSyntax ?? nodeToFix.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocationExpression is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (semanticModel.GetOperation(invocationExpression, context.CancellationToken) is not IInvocationOperation invocationOperation)
            return;

        var midpointRoundingSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.MidpointRounding");
        if (midpointRoundingSymbol is null)
            return;

        var overloadFinder = new OverloadFinder(semanticModel.Compilation);
        var overload = overloadFinder.FindOverloadWithAdditionalParameterOfType(invocationOperation, new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: true), [midpointRoundingSymbol]);
        if (overload is null)
            return;

        var midpointRoundingParameter = overload.Parameters.FirstOrDefault(parameter => parameter.Type.IsEqualTo(midpointRoundingSymbol));
        if (midpointRoundingParameter is null)
            return;

        var generator = SyntaxGenerator.GetGenerator(context.Document);
        foreach (var midpointRoundingMember in midpointRoundingSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (midpointRoundingMember is { IsImplicitlyDeclared: true, Name: "value__" })
                continue;

            if (!midpointRoundingMember.HasConstantValue)
                continue;

            var midpointRoundingMemberName = midpointRoundingMember.Name;
            var newInvocation = ArgumentListHelper.AddArgument(
                semanticModel,
                generator,
                invocationExpression,
                midpointRoundingParameter.Ordinal,
                midpointRoundingParameter.Name,
                generator.TypeMemberAccessExpression(midpointRoundingSymbol, midpointRoundingMemberName, addImport: true),
                parameter => parameter.Type.IsEqualTo(midpointRoundingSymbol),
                overload,
                cancellationToken: context.CancellationToken);

            if (newInvocation is null)
                continue;

            var title = "Add MidpointRounding." + midpointRoundingMemberName;
            var codeAction = CodeAction.Create(
                title,
                ct => AddMidpointRounding(context.Document, invocationExpression, newInvocation, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static async Task<Document> AddMidpointRounding(Document document, InvocationExpressionSyntax invocationExpression, InvocationExpressionSyntax newInvocation, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(invocationExpression, newInvocation);
        return editor.GetChangedDocument();
    }
}

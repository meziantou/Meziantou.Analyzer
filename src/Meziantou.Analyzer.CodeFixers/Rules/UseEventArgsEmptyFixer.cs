namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseEventArgsEmptyFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseEventArgsEmpty, RuleIdentifiers.EventArgsSenderShouldNotBeNullForEvents);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not ExpressionSyntax nodeToFix)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.Compilation.GetTypeByMetadataName("System.EventArgs") is not { } eventArgsSymbol)
            return;

        // The parameter can be typed with a type parameter constrained to EventArgs: Invoke(this, null) cannot become Invoke(this, EventArgs.Empty)
        if (semanticModel.GetTypeInfo(nodeToFix, context.CancellationToken).ConvertedType is { } convertedType && !semanticModel.Compilation.ClassifyConversion(eventArgsSymbol, convertedType).IsImplicit)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use EventArgs.Empty",
                ct => Fix(context.Document, nodeToFix, eventArgsSymbol, ct),
                equivalenceKey: "Use EventArgs.Empty"),
            context.Diagnostics);
    }

    private static async Task<Document> Fix(Document document, ExpressionSyntax nodeToFix, INamedTypeSymbol eventArgsSymbol, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var newExpression = editor.Generator.TypeMemberAccessExpression(eventArgsSymbol, nameof(EventArgs.Empty));
        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }
}

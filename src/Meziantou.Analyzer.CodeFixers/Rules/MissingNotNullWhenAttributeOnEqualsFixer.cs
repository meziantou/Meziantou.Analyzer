namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class MissingNotNullWhenAttributeOnEqualsFixer : CodeFixProvider
{
    private const string AttributeMetadataName = "System.Diagnostics.CodeAnalysis.NotNullWhenAttribute";
    private const string Title = "Add [NotNullWhen(true)]";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.MissingNotNullWhenAttributeOnEquals);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (!NullableAnalysisAttributeFixerHelper.TryGetParameterToFix(root, semanticModel, context.Span, AttributeMetadataName, nameof(object.Equals), context.CancellationToken, out var parameter, out _))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                ct => NullableAnalysisAttributeFixerHelper.AddOrUpdateAttributeAsync(context.Document, parameter, AttributeMetadataName, expectedValue: true, ct),
                equivalenceKey: Title),
            context.Diagnostics);
    }
}

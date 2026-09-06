namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class MissingMaybeNullWhenAttributeOnTryGetValueFixer : CodeFixProvider
{
    private const string AttributeMetadataName = "System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute";
    private const string MethodName = "TryGetValue";
    private const string Title = "Add [MaybeNullWhen(false)]";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.MissingMaybeNullWhenAttributeOnTryGetValue);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (!NullableAnalysisAttributeFixerHelper.TryGetParameterToFix(root, semanticModel, context.Span, AttributeMetadataName, MethodName, context.CancellationToken, out var parameter, out var parameterSymbol))
            return;

        if (parameterSymbol.RefKind != RefKind.Out)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                ct => NullableAnalysisAttributeFixerHelper.AddOrUpdateAttributeAsync(context.Document, parameter, AttributeMetadataName, expectedValue: false, ct),
                equivalenceKey: Title),
            context.Diagnostics);
    }
}

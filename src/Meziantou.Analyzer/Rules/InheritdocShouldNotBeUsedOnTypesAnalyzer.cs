namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InheritdocShouldNotBeUsedOnTypesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.InheritdocShouldNotBeUsedOnTypes,
        title: "Add dedicated documentation on types",
        messageFormat: "A type should have dedicated documentation instead of '<inheritdoc />'",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.InheritdocShouldNotBeUsedOnTypes));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSymbolAction(static context =>
        {
            InheritdocOnTypesAnalyzerHelper.Analyze(context, static (hasBaseType, interfaceCount) => hasBaseType || interfaceCount == 1, Rule);
        }, SymbolKind.NamedType);
    }
}

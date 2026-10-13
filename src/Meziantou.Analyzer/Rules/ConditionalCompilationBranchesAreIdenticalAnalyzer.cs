namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConditionalCompilationBranchesAreIdenticalAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ConditionalCompilationBranchesAreIdentical,
        title: "Conditional compilation branches have identical code",
        messageFormat: "Conditional compilation branches have identical code",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ConditionalCompilationBranchesAreIdentical));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
    }

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        if (!root.ContainsDirectives)
            return;

        var sourceText = context.Tree.GetText(context.CancellationToken);
        foreach (var group in ConditionalCompilationBranchesAreIdenticalCommon.GetBranchGroups(root, sourceText))
        {
            var firstBranchesWithSameContent = group.GetFirstBranchesWithSameContent();
            for (var i = 1; i < firstBranchesWithSameContent.Length; i++)
            {
                if (firstBranchesWithSameContent[i] != i)
                {
                    context.ReportDiagnostic(Rule, group.Branches[i].StartDirective.GetLocation());
                }
            }
        }
    }
}

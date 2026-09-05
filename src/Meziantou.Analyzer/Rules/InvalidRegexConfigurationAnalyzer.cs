using System.Text.RegularExpressions;
using Meziantou.Analyzer.Configurations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InvalidRegexConfigurationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.InvalidRegexConfiguration,
        title: "The configured regular expression is not valid",
        messageFormat: "The value of '{0}' is not a valid regular expression: {1}",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.InvalidRegexConfiguration));

    /// <summary>
    /// The options whose value must be a valid regular expression. The rules ignore an invalid value,
    /// so this rule is the only way for the user to know the option is not applied.
    /// </summary>
    private static readonly ConfigurationDefinition<string>[] RegexConfigurations =
    [
        NamedParameterAnalyzer.ExcludedMethodsRegexConfiguration,
        DotNotUseNameFromBCLAnalyzer.NamespacesRegexConfiguration,
        DotNotUseNameFromBCLAnalyzer.LegacyNamepacesRegexConfiguration,
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        // The options can be different for each syntax tree, but a value must be reported only once
        HashSet<(string Key, string Value)>? reportedValues = null;

        foreach (var syntaxTree in context.Compilation.SyntaxTrees)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
            foreach (var configuration in RegexConfigurations)
            {
                if (!options.TryGetValue(configuration.Key, out var value))
                    continue;

                reportedValues ??= [];
                if (!reportedValues.Add((configuration.Key, value)))
                    continue;

                if (!RegexCache.IsValidPattern(value, RegexOptions.None, out var errorMessage))
                {
                    context.ReportDiagnostic(Rule, Location.None, configuration.Key, errorMessage);
                }
            }
        }
    }
}

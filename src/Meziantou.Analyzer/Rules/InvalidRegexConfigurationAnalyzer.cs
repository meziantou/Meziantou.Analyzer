using System.Reflection;
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

    private static readonly ConfigurationDefinition<string>[] RegexConfigurations = GetRegexConfigurations();

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <summary>
    /// Gets the options whose value must be a valid regular expression. The rules ignore an invalid value, so this rule
    /// is the only way for the user to know the option is not applied. The options are discovered from the assembly, so
    /// a new option is validated as soon as its <see cref="ConfigurationDefinition{T}.RegexOptions"/> are set.
    /// </summary>
    internal static ConfigurationDefinition<string>[] GetRegexConfigurations()
    {
        var configurations = new List<ConfigurationDefinition<string>>();
        foreach (var type in typeof(InvalidRegexConfigurationAnalyzer).Assembly.GetTypes())
        {
            foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.FieldType != typeof(ConfigurationDefinition<string>))
                    continue;

                if (field.GetValue(null) is ConfigurationDefinition<string> { IsRegex: true } configuration)
                {
                    configurations.Add(configuration);
                }
            }
        }

        // The order of the fields is not deterministic, so sort the configurations to report the diagnostics in a stable order
        return [.. configurations.OrderBy(configuration => configuration.Key, StringComparer.Ordinal)];
    }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        // The options can be different for each syntax tree, but a value must be reported only once
        HashSet<(string Key, string Value, RegexOptions Options)>? reportedValues = null;

        foreach (var syntaxTree in context.Compilation.SyntaxTrees)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
            foreach (var configuration in RegexConfigurations)
            {
                // The legacy names of an option are validated too, as they are still supported
                foreach (var key in configuration.Keys)
                {
                    if (!options.TryGetValue(key, out var value))
                        continue;

                    reportedValues ??= [];
                    if (!reportedValues.Add((key, value, configuration.RegexOptions)))
                        continue;

                    if (!RegexCache.IsValidPattern(value, configuration.RegexOptions, out var errorMessage))
                    {
                        context.ReportDiagnostic(Rule, Location.None, key, errorMessage);
                    }
                }
            }
        }
    }
}

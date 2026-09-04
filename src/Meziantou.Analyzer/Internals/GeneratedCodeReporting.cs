using Meziantou.Analyzer.Configurations;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Decides whether a rule reports the diagnostics located in generated code. The analyzers configure Roslyn to
/// analyze generated code and to report the diagnostics located in it, as <c>ConfigureGeneratedCodeAnalysis</c>
/// cannot read the <c>.editorconfig</c> options, so the decision is taken by
/// <see cref="DiagnosticReportingExtensions"/> when a diagnostic is reported.
/// </summary>
internal static class GeneratedCodeReporting
{
    /// <summary>
    /// The tag of the rules that report in generated code when nothing is configured, such as the rules
    /// whose subject is the generated file itself.
    /// </summary>
    public const string ReportInGeneratedCodeTag = "ReportInGeneratedCode";

    /// <summary>
    /// The configuration key that applies to all the rules, such as <c>MA.report_generated_code = true</c>.
    /// A rule specific key wins over it.
    /// </summary>
    public const string GlobalConfigurationKey = "MA" + ConfigurationKeySuffix;

    private const string ConfigurationKeySuffix = ".report_generated_code";

    public static bool CanReportDiagnostic(AnalyzerOptions options, DiagnosticDescriptor descriptor, SyntaxTree? syntaxTree, CancellationToken cancellationToken)
    {
        // A diagnostic without a location in the source code, such as one reported on a symbol coming from
        // metadata, is not in generated code
        if (syntaxTree is null)
            return true;

        if (!syntaxTree.IsGeneratedCode(options, cancellationToken))
            return true;

        return IsReportingEnabled(options, descriptor, syntaxTree);
    }

    private static bool IsReportingEnabled(AnalyzerOptions options, DiagnosticDescriptor descriptor, SyntaxTree syntaxTree)
    {
        if (TryGetConfiguration(options, syntaxTree, descriptor.Id + ConfigurationKeySuffix, out var enabled))
            return enabled;

        if (TryGetConfiguration(options, syntaxTree, GlobalConfigurationKey, out enabled))
            return enabled;

        return descriptor.CustomTags.Contains(ReportInGeneratedCodeTag, StringComparer.Ordinal);
    }

    private static bool TryGetConfiguration(AnalyzerOptions options, SyntaxTree syntaxTree, string key, out bool value)
    {
        if (options.TryGetConfigurationValue(syntaxTree, key, out var configuredValue))
            return bool.TryParse(configuredValue, out value);

        value = false;
        return false;
    }
}

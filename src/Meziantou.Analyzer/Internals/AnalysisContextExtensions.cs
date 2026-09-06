// ConfigureGeneratedCodeAnalysis must be called from Initialize(AnalysisContext), where AnalyzerOptions
// (.editorconfig) are not available, so an environment variable is the only way to let users configure it.
// This is the documented exception to RS1035.
#pragma warning disable RS1035 // Do not use APIs banned for analyzers

namespace Meziantou.Analyzer.Internals;

internal static class AnalysisContextExtensions
{
    internal const string EnvironmentVariableName = "MEZIANTOU_ANALYZER_GENERATED_CODE";

    // Static field initializers run once and are thread-safe, and the value cannot change during the
    // lifetime of the process, so no lock or Lazy<T> is needed.
    private static readonly GeneratedCodeAnalysisFlags AdditionalFlags = GetAdditionalFlags(ReadEnvironmentVariable());

    /// <summary>
    /// Configures how the analyzer handles generated code. The rules analyze generated code and report the
    /// diagnostics located in it, which <see cref="GeneratedCodeReporting"/> then filters, unless the
    /// <c>MEZIANTOU_ANALYZER_GENERATED_CODE</c> environment variable opts out, as analyzing generated code has a
    /// cost. <paramref name="defaultFlags"/> is the minimum, which opting out cannot remove: it is what the rule
    /// needs to be correct, or to report in generated code by default.
    /// </summary>
    public static void ConfigureAnalysisOfGeneratedCode(this AnalysisContext context, GeneratedCodeAnalysisFlags defaultFlags)
    {
#pragma warning disable RS0030 // Do not use banned APIs
        context.ConfigureGeneratedCodeAnalysis(defaultFlags | AdditionalFlags);
#pragma warning restore RS0030
    }

    internal static GeneratedCodeAnalysisFlags GetAdditionalFlags(string? value) => value?.Trim() switch
    {
        "0" => GeneratedCodeAnalysisFlags.None,
        var trimmed when string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase) => GeneratedCodeAnalysisFlags.None,
        _ => GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics,
    };

    private static string? ReadEnvironmentVariable()
    {
        // An exception in a static initializer would surface as a TypeInitializationException on every
        // Initialize call and break every rule (AD0001)
        try
        {
            return Environment.GetEnvironmentVariable(EnvironmentVariableName);
        }
        catch (Exception)
        {
            return null;
        }
    }
}

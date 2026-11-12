namespace Meziantou.Analyzer.Internals;

internal static class DiagnosticDescriptorExtensions
{
    /// <summary>
    /// Indicates whether the compiler drops the diagnostics of <paramref name="descriptor"/> reported in <paramref name="syntaxTree"/>,
    /// so that a rule can skip the work only needed to report them, such as a rule disabled by default. This follows the
    /// order of the compiler: the compilation options (<c>/nowarn</c>, <c>/warnaserror</c>, ruleset), then the
    /// <c>dotnet_diagnostic.&lt;id&gt;.severity</c> options of the <c>.editorconfig</c> and <c>.globalconfig</c> files,
    /// then the default of the rule. The <c>dotnet_analyzer_diagnostic.*.severity</c> options are not read, as they only apply
    /// to the rules enabled by default. The <c>#pragma warning</c> directives are not considered, as they cannot enable a rule.
    /// </summary>
    public static bool IsSuppressed(this DiagnosticDescriptor descriptor, Compilation compilation, SyntaxTree syntaxTree, CancellationToken cancellationToken)
    {
        var options = compilation.Options;
        var isSpecified = options.SpecificDiagnosticOptions.TryGetValue(descriptor.Id, out var severity);
        if (!isSpecified || severity is ReportDiagnostic.Default)
        {
            if (options.SyntaxTreeOptionsProvider is { } syntaxTreeOptionsProvider &&
                (syntaxTreeOptionsProvider.TryGetDiagnosticValue(syntaxTree, descriptor.Id, cancellationToken, out var treeSeverity) ||
                 syntaxTreeOptionsProvider.TryGetGlobalDiagnosticValue(descriptor.Id, cancellationToken, out treeSeverity)))
            {
                isSpecified = true;
                severity = treeSeverity;
            }
        }

        if (!isSpecified)
            return !descriptor.IsEnabledByDefault;

        return severity is ReportDiagnostic.Suppress;
    }
}

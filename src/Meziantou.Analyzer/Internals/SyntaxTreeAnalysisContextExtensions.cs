// SyntaxTreeAnalysisContext is not supported by DiagnosticReporter, so the diagnostics reported from it do not
// go through the filter of GeneratedCodeReporting and must be reported by this class
#pragma warning disable RS0030 // Do not use banned APIs

namespace Meziantou.Analyzer.Internals;

internal static class SyntaxTreeAnalysisContextExtensions
{
    public static void ReportDiagnostic(this SyntaxTreeAnalysisContext context, DiagnosticDescriptor descriptor, Location location, params object?[]? messageArgs)
    {
        if (!GeneratedCodeReporting.CanReportDiagnostic(context.Options, descriptor, location.SourceTree, context.CancellationToken))
            return;

        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
    }
}

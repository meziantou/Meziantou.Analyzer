using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Meziantou.Analyzer.Test.Harness;

/// <summary>
/// A <see cref="Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest{TAnalyzer, TVerifier}"/> configured with the
/// defaults of this repository. Set <c>TestCode</c>, then call <c>RunAsync</c>. The source code uses the same
/// <c>[|code|]</c> and <c>{|ruleId:code|}</c> markup as <see cref="TestHelper.ProjectBuilder"/>, except that
/// <c>[|code|]</c> requires the analyzer to support a single rule.
/// </summary>
internal sealed class CSharpAnalyzerTest<TAnalyzer>
    : Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public CSharpAnalyzerTest()
    {
        ReferenceAssemblies = AnalyzerTestDefaults.ReferenceAssemblies;
        SolutionTransforms.Add(AnalyzerTestDefaults.ConfigureCompilationOptions);
    }

    /// <summary>
    /// Runs the test with the cancellation token of the running test, which the base method cannot default to.
    /// This overload wins over the inherited <c>RunAsync(CancellationToken)</c> as it has no optional parameter.
    /// </summary>
    public Task RunAsync() => RunAsync(TestContext.Current.CancellationToken);

    /// <summary>
    /// The analyzers to run in addition to <typeparamref name="TAnalyzer"/>, such as the external analyzers
    /// producing the diagnostics a <see cref="Microsoft.CodeAnalysis.Diagnostics.DiagnosticSuppressor"/> suppresses.
    /// </summary>
    public IList<DiagnosticAnalyzer> AdditionalAnalyzers { get; } = [];

    protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers() =>
        [.. base.GetDiagnosticAnalyzers(), .. AdditionalAnalyzers];

    /// <summary>
    /// The language version the code is parsed with.
    /// </summary>
    public LanguageVersion LanguageVersion { get; set; } = AnalyzerTestDefaults.LanguageVersion;

    protected override ParseOptions CreateParseOptions() =>
        ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
}

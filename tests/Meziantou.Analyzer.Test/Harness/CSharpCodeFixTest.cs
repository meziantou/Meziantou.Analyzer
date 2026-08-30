using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Meziantou.Analyzer.Test.Harness;

/// <summary>
/// A <see cref="Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest{TAnalyzer, TCodeFix, TVerifier}"/> configured
/// with the defaults of this repository. Set <c>TestCode</c>, <c>FixedCode</c> or <c>BatchFixedCode</c>, then call
/// <c>RunAsync</c>. The source code uses the same <c>[|code|]</c> and <c>{|ruleId:code|}</c> markup as
/// <see cref="TestHelper.ProjectBuilder"/>.
/// </summary>
internal sealed class CSharpCodeFixTest<TAnalyzer, TCodeFix>
    : Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public CSharpCodeFixTest()
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
    /// The language version the code is parsed with.
    /// </summary>
    public LanguageVersion LanguageVersion { get; set; } = AnalyzerTestDefaults.LanguageVersion;

    protected override ParseOptions CreateParseOptions() =>
        ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
}

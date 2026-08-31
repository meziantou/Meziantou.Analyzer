using Meziantou.Analyzer.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Meziantou.Analyzer.Test.Harness;

/// <summary>
/// A <see cref="Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest{TAnalyzer, TCodeFix, TVerifier}"/> configured
/// with the defaults of this repository. Set <c>TestCode</c>, <c>FixedCode</c> or <c>BatchFixedCode</c>, then call
/// <c>RunAsync</c>. The source code uses the same <c>[|code|]</c> and <c>{|ruleId:code|}</c> markup.
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
    [ExcludeFromCancellationTokenAnalysis]
    public Task RunAsync() => RunAsync(TestContext.Current.CancellationToken);

    /// <summary>
    /// The analyzers to run in addition to <typeparamref name="TAnalyzer"/>, such as the external analyzers
    /// producing the diagnostics a <see cref="Microsoft.CodeAnalysis.Diagnostics.DiagnosticSuppressor"/> suppresses.
    /// </summary>
    public IList<DiagnosticAnalyzer> AdditionalAnalyzers { get; } = [];

    protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers() =>
        [.. base.GetDiagnosticAnalyzers(), .. AdditionalAnalyzers];

    /// <summary>
    /// Runs the source generators shipped with the .NET reference pack the test compiles against, so that the
    /// partial members the generators implement do not need a hand written implementation.
    /// </summary>
    public bool UseFrameworkSourceGenerators { get; set; }

    protected override IEnumerable<Type> GetSourceGenerators() =>
        UseFrameworkSourceGenerators
            ? [.. base.GetSourceGenerators(), .. AnalyzerTestDefaults.GetFrameworkSourceGenerators(ReferenceAssemblies)]
            : base.GetSourceGenerators();

    protected override async Task RunImplAsync(CancellationToken cancellationToken)
    {
        // The tests use the generators to compile the code, not to assert what they produce
        if (UseFrameworkSourceGenerators)
        {
            TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;
        }

        await base.RunImplAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The language version the code is parsed with.
    /// </summary>
    public LanguageVersion LanguageVersion { get; set; } = AnalyzerTestDefaults.LanguageVersion;

    protected override ParseOptions CreateParseOptions() =>
        ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace Meziantou.Analyzer.Test.Harness;

/// <summary>
/// A <see cref="Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeRefactoringTest{TCodeRefactoring, TVerifier}"/>
/// configured with the defaults of this repository. Set <c>TestCode</c>, where <c>[|code|]</c> is the selection the
/// refactoring is requested on, and <c>FixedCode</c>, then call <c>RunAsync</c>.
/// </summary>
internal sealed class CSharpCodeRefactoringTest<TCodeRefactoring>
    : Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeRefactoringTest<TCodeRefactoring, DefaultVerifier>
    where TCodeRefactoring : CodeRefactoringProvider, new()
{
    public CSharpCodeRefactoringTest()
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

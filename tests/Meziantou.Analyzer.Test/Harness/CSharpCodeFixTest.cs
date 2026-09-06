using System.Collections.Immutable;
using Meziantou.Analyzer.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
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

        // The base implementation only invokes the code fix provider when the test declares the fixed code
        if (!CodeActionExpected(FixedState) && !CodeActionExpected(BatchFixedState))
        {
            await VerifyCodeFixActionsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The diagnostics of the test code, captured the first time they are computed, which is the verification of
    /// the test state. The later computations are the ones of the <c>&lt;auto-generated&gt;</c> and the
    /// <c>#pragma warning disable</c> variants of the code, which are not the code the test declares.
    /// </summary>
    private ImmutableArray<(Project Project, Diagnostic Diagnostic)>? _testStateDiagnostics;

    protected override ImmutableArray<(Project project, Diagnostic diagnostic)> SortDistinctDiagnostics(ImmutableArray<(Project project, Diagnostic diagnostic)> diagnostics)
    {
        var sortedDiagnostics = base.SortDistinctDiagnostics(diagnostics);
        _testStateDiagnostics ??= sortedDiagnostics;
        return sortedDiagnostics;
    }

    /// <summary>
    /// Registers the code fixes for every reported diagnostic the provider declares as fixable, and computes the
    /// changes of every registered action, without applying them.
    /// </summary>
    /// <remarks>
    /// A test that configures a code fix provider but no fixed code never invokes the provider otherwise, so a
    /// provider that throws for that shape of code goes unnoticed, even though those snippets are inputs the
    /// provider does run on in an IDE. Computing the changes is what runs the delegate the provider registered,
    /// where the fix actually builds the new document. They cannot be compared, as the test did not declare what
    /// the fixed code should be, but they must be computable.
    /// </remarks>
    private async Task VerifyCodeFixActionsAsync(CancellationToken cancellationToken)
    {
        if (_testStateDiagnostics is not { } diagnostics)
            return;

        var codeFixProviders = GetCodeFixProviders().ToArray();
        foreach (var (project, diagnostic) in diagnostics)
        {
            if (project.Solution.GetDocument(diagnostic.Location.SourceTree) is not { } document)
                continue;

            foreach (var codeFixProvider in codeFixProviders)
            {
                if (!codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id, StringComparer.Ordinal))
                    continue;

                var actions = new List<CodeAction>();
                var context = CreateCodeFixContext(document, diagnostic.Location.SourceSpan, [diagnostic], (action, _) => actions.Add(action), cancellationToken);
                await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

                foreach (var action in FilterCodeActions([.. actions]))
                {
                    await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    /// The language version the code is parsed with.
    /// </summary>
    public LanguageVersion LanguageVersion { get; set; } = AnalyzerTestDefaults.LanguageVersion;

    protected override ParseOptions CreateParseOptions() =>
        ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
}

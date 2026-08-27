using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace TestHelper;

/// <summary>
/// Runs the test described by a <see cref="ProjectBuilder"/> with the Roslyn test framework
/// (<c>Microsoft.CodeAnalysis.CSharp.Analyzer.Testing</c> and <c>Microsoft.CodeAnalysis.CSharp.CodeFix.Testing</c>).
/// </summary>
/// <remarks>
/// The framework provides <see cref="Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest{TAnalyzer, TCodeFix, TVerifier}"/>,
/// but it takes the analyzer and the code fixer as type parameters. The tests configure them as instances, so this
/// class derives from the language-agnostic <see cref="CodeFixTest{TVerifier}"/> and provides the C# specific members.
/// </remarks>
internal sealed class ProjectBuilderTest : CodeFixTest<DefaultVerifier>
{
    private readonly ProjectBuilder _projectBuilder;
    private readonly bool _includeCodeFix;
    private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
    private readonly ImmutableHashSet<string> _analyzerDiagnosticIds;
    private Project? _testProject;

    /// <param name="includeCodeFix">
    /// <see langword="false"/> to only validate the diagnostics reported by the analyzers, ignoring the code fixer
    /// configured by the <see cref="ProjectBuilder"/>.
    /// </param>
    public ProjectBuilderTest(ProjectBuilder projectBuilder, bool includeCodeFix = true)
    {
        _projectBuilder = projectBuilder;
        _includeCodeFix = includeCodeFix;

        _analyzers = projectBuilder.GeneratedCodeAnalysisFlags is { } flags
            ? [.. projectBuilder.DiagnosticAnalyzer.Select(analyzer => (DiagnosticAnalyzer)new GeneratedCodeAnalysisAnalyzer(analyzer, flags))]
            : [.. projectBuilder.DiagnosticAnalyzer];
        _analyzerDiagnosticIds = [.. _analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics).Select(descriptor => descriptor.Id)];

        // The references are downloaded by the ProjectBuilder, so the framework must not add its own
        ReferenceAssemblies = new ReferenceAssemblies(projectBuilder.TargetFramework.ToTargetFrameworkMoniker());
        TestState.AdditionalReferences.AddRange(projectBuilder.References);
        TestState.OutputKind = projectBuilder.OutputKind;

        foreach (var (fileName, content) in projectBuilder.GetSources())
        {
            TestState.Sources.Add((fileName, content));
        }

        if (projectBuilder.AdditionalFiles is not null)
        {
            foreach (var (path, content) in projectBuilder.AdditionalFiles)
            {
                TestState.AdditionalFiles.Add((path, content));
            }
        }

        TestState.ExpectedDiagnostics.AddRange(projectBuilder.GetExpectedDiagnostics(_analyzers));

        // Source generators are added as analyzer references so that they run as part of the compilation of the
        // project, the way they do in a real build. Adding them through GetSourceGenerators would instead add their
        // output to the project as regular documents, which the code fixers would then try to fix.
        var analyzerReferences = projectBuilder.AnalyzerReferences.DistinctBy(reference => reference.FullPath, StringComparer.Ordinal).ToArray();
        if (analyzerReferences.Length > 0)
        {
            SolutionTransforms.Add((solution, projectId) =>
            {
                foreach (var analyzerReference in analyzerReferences)
                {
                    solution = solution.AddAnalyzerReference(projectId, analyzerReference);
                }

                return solution;
            });
        }

        CompilerDiagnostics = projectBuilder.IsValidCode ? CompilerDiagnostics.Errors : CompilerDiagnostics.None;
        CodeActionValidationMode = CodeActionValidationMode.None;

        // How the analyzers handle generated code is tested by GeneratedCodeAnalysisTests, and the check that a
        // diagnostic can be suppressed with '#pragma warning disable' analyzes the code under test a second time,
        // which would double the duration of every test that reports a diagnostic
        TestBehaviors |= TestBehaviors.SkipGeneratedCodeCheck | TestBehaviors.SkipSuppressionCheck;

        if (includeCodeFix && projectBuilder.ExpectedFixedCode is not null)
        {
            var sources = projectBuilder.GetSources().ToArray();
            if (projectBuilder.ExpectedFixedCode.Length == 0)
            {
                // An empty fixed code means the code fixer removes the document. The framework compares the
                // documents of the project with the sources of the fixed state, so the fixed state must not
                // declare the document, which also means it inherits nothing from the state under test.
                FixedState.InheritanceMode = StateInheritanceMode.Explicit;
                FixedState.OutputKind = projectBuilder.OutputKind;
                FixedState.AdditionalReferences.AddRange(projectBuilder.References);
                AddFixedSources(FixedState, codeUnderTest: null);
            }
            else
            {
                AddFixedSources(FixedState, projectBuilder.ExpectedFixedCode);
            }

            // The fixed code can declare the diagnostics that remain after the fix with the [|code|] and
            // {|ruleId:code|} syntaxes, including the ones the code fixer could fix
            FixedState.MarkupHandling = MarkupMode.Allow;

            // Only the code under test is fixed, the sources of the API references are expected to be unchanged
            void AddFixedSources(SolutionState state, string? codeUnderTest)
            {
                if (codeUnderTest is not null)
                {
                    state.Sources.Add((sources[0].FileName, codeUnderTest));
                }

                foreach (var (fileName, content) in sources.Skip(1))
                {
                    state.Sources.Add((fileName, content));
                }
            }

            CodeActionIndex = projectBuilder.CodeFixIndex;
            if (projectBuilder.CodeFixIndex is not null || projectBuilder.FixFirstDiagnosticOnly)
            {
                // A test that selects a code action by its index expects that action to be applied once
                CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne;
            }

            // Several analyzers of this repository report their diagnostics from a compilation end action, so
            // their diagnostics are not local and the framework would refuse to fix them
            CodeFixTestBehaviors |= CodeFixTestBehaviors.SkipLocalDiagnosticCheck;

            if (!projectBuilder.UseBatchFixer)
            {
                // The fix all providers are only tested by the tests using ShouldBatchFixCodeWith. BatchFixedState
                // inherits FixedState, so those tests expect the fix all provider to produce the same code.
                CodeFixTestBehaviors |= CodeFixTestBehaviors.SkipFixAllCheck;
            }

            if (!projectBuilder.IsValidFixCode)
            {
                // The framework uses a single setting for the code under test and for the fixed code, so the code
                // under test is compiled by the analyzer only run of ProjectBuilder.ValidateAsync instead
                CompilerDiagnostics = CompilerDiagnostics.None;
            }
        }
    }

    public override string Language => LanguageNames.CSharp;

    protected override string DefaultFileExt => "cs";

    public override Type SyntaxKindType => typeof(SyntaxKind);

    protected override CompilationOptions CreateCompilationOptions()
        => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, metadataImportOptions: MetadataImportOptions.All);

    protected override ParseOptions CreateParseOptions()
        => CSharpParseOptions.Default.WithLanguageVersion(_projectBuilder.LanguageVersion);

    protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers() => _analyzers;

    protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
        => _includeCodeFix && _projectBuilder.CodeFixProvider is { } codeFixProvider ? [codeFixProvider] : [];

    protected override AnalyzerOptions GetAnalyzerOptions(Project project)
    {
        var options = base.GetAnalyzerOptions(project);
        if (_projectBuilder.AnalyzerConfiguration is null)
            return options;

        return new AnalyzerOptions(options.AdditionalFiles, new TestAnalyzerConfigOptionsProvider(_projectBuilder.AnalyzerConfiguration, options.AnalyzerConfigOptionsProvider));
    }

    protected override ImmutableArray<(Project project, Diagnostic diagnostic)> FilterDiagnostics(ImmutableArray<(Project project, Diagnostic diagnostic)> diagnostics)
    {
        // The framework keeps the diagnostics suppressed by a DiagnosticSuppressor so that a test can assert that
        // they are suppressed. The tests of this repository assert that they are not reported at all.
        var result = base.FilterDiagnostics(diagnostics).Where(item => !item.diagnostic.IsSuppressed);

        if (_projectBuilder.DefaultAnalyzerId is { } defaultAnalyzerId)
        {
            result = result.Where(item => item.diagnostic.Id == defaultAnalyzerId || !_analyzerDiagnosticIds.Contains(item.diagnostic.Id));
        }

        return [.. result];
    }

    protected override Task<(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics)> GetProjectCompilationAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
    {
        _testProject ??= project;
        return base.GetProjectCompilationAsync(project, verifier, cancellationToken);
    }

    protected override async Task RunImplAsync(CancellationToken cancellationToken)
    {
        await base.RunImplAsync(cancellationToken).ConfigureAwait(false);

        if (_includeCodeFix && _projectBuilder.CodeFixProvider is { } codeFixProvider && _projectBuilder.ExpectedFixedCode is null)
        {
            await VerifyCodeFixActionsAsync(codeFixProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Registers the code fixes for every reported diagnostic and computes the changes of every registered
    /// action, without applying them.
    /// </summary>
    /// <remarks>
    /// A test that configures a code fix provider but no expected fixed code never invokes the provider
    /// otherwise, so a provider that throws for that shape of code goes unnoticed. The changes cannot be
    /// compared, as the test did not declare what the fixed code should be, but they must be computable.
    /// </remarks>
    private async Task VerifyCodeFixActionsAsync(CodeFixProvider codeFixProvider, CancellationToken cancellationToken)
    {
        if (_testProject is null)
            return;

        var compilation = await _testProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        var compilationWithAnalyzers = compilation!.WithAnalyzers(_analyzers, GetAnalyzerOptions(_testProject));
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken).ConfigureAwait(false);

        foreach (var document in _testProject.Documents)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Location.SourceTree != syntaxTree)
                    continue;

                if (!codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id, StringComparer.Ordinal))
                    continue;

                var actions = new List<CodeAction>();
                var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), cancellationToken);
                await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

                // Computing the operations is what runs the delegate a code fix provider registered. A provider
                // often registers several actions, and applying one of them exercises only that one.
                foreach (var action in actions)
                {
                    await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}

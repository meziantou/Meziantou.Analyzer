using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Fuzz;

/// <summary>
/// Run the analyzers on a C# file, then the code fixers on the diagnostics they report, and collect the defects.
/// </summary>
internal sealed class AnalysisEngine(ComponentFilter filter, int maxFixedDiagnostics = 20)
{
    public async Task<IReadOnlyList<Finding>> RunAsync(string source, CancellationToken cancellationToken = default)
    {
        var findings = new List<Finding>();
        try
        {
            using var workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            var solution = workspace.CurrentSolution
                .AddProject(ProjectInfo.Create(
                    projectId,
                    VersionStamp.Default,
                    name: "Fuzz",
                    assemblyName: "Fuzz",
                    LanguageNames.CSharp,
                    compilationOptions: Components.CompilationOptions,
                    parseOptions: Components.ParseOptions,
                    metadataReferences: Components.MetadataReferences,
                    analyzerReferences: Components.SourceGenerators))
                .AddDocument(documentId, "Fuzz.cs", SourceText.From(source));

            var document = solution.GetDocument(documentId)!;
            var compilation = (await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false))!;

            // The compiler can throw on invalid code, such as "[assembly:(async()=>", and then hangs when the compilation is used again,
            // as the symbols it was completing are never completed. The analyzers cannot be blamed for it, so the input is not analyzed.
            ImmutableArray<Diagnostic> compilerDiagnostics;
            try
            {
                compilerDiagnostics = compilation.GetDiagnostics(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                findings.Add(Finding.FromException(FindingKind.RoslynException, "Microsoft.CodeAnalysis.CSharp", [], ex));
                return findings;
            }

            var diagnostics = await RunAnalyzersAsync(compilation, findings, cancellationToken).ConfigureAwait(false);

            // The parser can produce a syntax tree that does not contain the whole text, such as "r e implicit operator" whose tree does not contain "e ".
            // The workspace services used by the code fixers, such as SymbolFinder and Renamer, throw on such a document.
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!filter.CodeFixProviders.IsEmpty && root?.FullSpan.Length == source.Length)
            {
                var hasErrors = compilerDiagnostics.Any(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error);
                await RunCodeFixesAsync(document, diagnostics, hasErrors, findings, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            findings.Add(Finding.FromException(FindingKind.HarnessException, "Meziantou.Analyzer.Fuzz", [], ex));
        }

        return [.. findings.DistinctBy(finding => finding.Signature)];
    }

    private async Task<ImmutableArray<Diagnostic>> RunAnalyzersAsync(Compilation compilation, List<Finding> findings, CancellationToken cancellationToken)
    {
        if (filter.Analyzers.IsEmpty)
            return [];

        var exceptions = new ConcurrentQueue<(DiagnosticAnalyzer Analyzer, Exception Exception)>();
        var options = new CompilationWithAnalyzersOptions(
            new AnalyzerOptions([]),
            onAnalyzerException: (exception, analyzer, _) => exceptions.Enqueue((analyzer, exception)),
            concurrentAnalysis: false, // Keep the coverage deterministic, so libFuzzer can tell which mutations are interesting
            logAnalyzerExecutionTime: false,
            reportSuppressedDiagnostics: false);

        var compilationWithAnalyzers = compilation.WithAnalyzers(filter.Analyzers, options);
        var diagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
        foreach (var (analyzer, exception) in exceptions)
        {
            if (exception is OperationCanceledException)
                continue;

            findings.Add(Finding.FromException(FindingKind.AnalyzerException, analyzer, Components.GetDiagnosticIds(analyzer), exception));
        }

        return diagnostics;
    }

    private async Task RunCodeFixesAsync(Document document, ImmutableArray<Diagnostic> diagnostics, bool hasErrors, List<Finding> findings, CancellationToken cancellationToken)
    {
        var fixableDiagnostics = diagnostics
            .Where(diagnostic => diagnostic.Location.IsInSource && !diagnostic.IsSuppressed && filter.CodeFixProviders.ContainsKey(diagnostic.Id))
            .DistinctBy(diagnostic => (diagnostic.Id, diagnostic.Location.SourceSpan))
            .Take(maxFixedDiagnostics);

        foreach (var diagnostic in fixableDiagnostics)
        {
            foreach (var provider in filter.CodeFixProviders[diagnostic.Id])
            {
                await RunCodeFixAsync(document, provider, diagnostic, hasErrors, findings, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task RunCodeFixAsync(Document document, CodeFixProvider provider, Diagnostic diagnostic, bool hasErrors, List<Finding> findings, CancellationToken cancellationToken)
    {
        try
        {
            var actions = new List<CodeAction>();
            var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), cancellationToken);
            await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            foreach (var action in Flatten(actions))
            {
                var operations = await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
                var changedSolution = operations.OfType<ApplyChangesOperation>().LastOrDefault()?.ChangedSolution;
                if (changedSolution is null || !await HasTextChangesAsync(document.Project.Solution, changedSolution, cancellationToken).ConfigureAwait(false))
                {
                    findings.Add(new Finding
                    {
                        Kind = FindingKind.CodeFixDoesNotChangeCode,
                        Component = Components.GetName(provider),
                        DiagnosticIds = [diagnostic.Id],
                        CodeActionTitle = action.Title,
                    });
                    continue;
                }

                // The fuzzed code rarely compiles, and a code fix cannot be expected to fix the errors of the code
                if (hasErrors || changedSolution.GetProject(document.Project.Id) is not { } changedProject)
                    continue;

                var changedCompilation = (await changedProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false))!;
                var errors = changedCompilation.GetDiagnostics(cancellationToken).Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error).ToList();
                if (errors.Count > 0)
                {
                    var fixedDocument = changedSolution.GetDocument(document.Id);
                    findings.Add(new Finding
                    {
                        Kind = FindingKind.CodeFixBreaksCode,
                        Component = Components.GetName(provider),
                        DiagnosticIds = [diagnostic.Id],
                        CompilerErrors = [.. errors.Select(error => error.Id).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)],
                        Message = string.Join('\n', errors.Take(10).Select(error => error.ToString())),
                        CodeActionTitle = action.Title,
                        FixedSource = fixedDocument is null ? null : (await fixedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString(),
                    });
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            findings.Add(Finding.FromException(FindingKind.CodeFixException, provider, [diagnostic.Id], ex));
        }
    }

    private static IEnumerable<CodeAction> Flatten(IEnumerable<CodeAction> actions)
    {
        foreach (var action in actions)
        {
            if (action.NestedActions.IsEmpty)
            {
                yield return action;
                continue;
            }

            foreach (var nestedAction in Flatten(action.NestedActions))
            {
                yield return nestedAction;
            }
        }
    }

    private static async Task<bool> HasTextChangesAsync(Solution originalSolution, Solution changedSolution, CancellationToken cancellationToken)
    {
        var changes = changedSolution.GetChanges(originalSolution);
        if (changes.GetAddedProjects().Any() || changes.GetRemovedProjects().Any())
            return true;

        foreach (var projectChanges in changes.GetProjectChanges())
        {
            if (projectChanges.GetAddedDocuments().Any() || projectChanges.GetRemovedDocuments().Any()
                || projectChanges.GetAddedAdditionalDocuments().Any() || projectChanges.GetRemovedAdditionalDocuments().Any()
                || projectChanges.GetChangedAdditionalDocuments().Any() || projectChanges.GetAddedAnalyzerConfigDocuments().Any()
                || projectChanges.GetRemovedAnalyzerConfigDocuments().Any() || projectChanges.GetChangedAnalyzerConfigDocuments().Any()
                || projectChanges.GetAddedMetadataReferences().Any() || projectChanges.GetRemovedMetadataReferences().Any())
            {
                return true;
            }

            foreach (var documentId in projectChanges.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true))
            {
                var originalText = await originalSolution.GetDocument(documentId)!.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var changedText = await changedSolution.GetDocument(documentId)!.GetTextAsync(cancellationToken).ConfigureAwait(false);
                if (!originalText.ContentEquals(changedText))
                    return true;
            }
        }

        return false;
    }
}

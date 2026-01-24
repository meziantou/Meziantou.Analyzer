using System.Collections.Immutable;
using System.Diagnostics;
using Meziantou.Analyzer.Internals;
using Meziantou.Analyzer.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace TestHelper;

public sealed partial class ProjectBuilder
{
    public async Task ValidateAsync()
    {
        if (DiagnosticAnalyzer is null)
        {
            Assert.Fail("DiagnosticAnalyzer is not configured");
        }

        if (ExpectedFixedCode is not null && CodeFixProvider is null)
        {
            Assert.Fail("CodeFixProvider is not configured");
        }

        if (ExpectedDiagnosticResults is null)
        {
            Assert.Fail("ExpectedDiagnostic is not configured");
        }

        await VerifyDiagnostic(ExpectedDiagnosticResults).ConfigureAwait(false);

        if (ExpectedFixedCode is not null)
        {
            await VerifyFix(DiagnosticAnalyzer, CodeFixProvider!, ExpectedFixedCode, CodeFixIndex).ConfigureAwait(false);
        }
    }

    [DebuggerStepThrough]
    private Task VerifyDiagnostic(IList<DiagnosticResult> expected)
    {
        return VerifyDiagnostics(DiagnosticAnalyzer, expected);
    }

    [DebuggerStepThrough]
    private async Task VerifyDiagnostics(IList<DiagnosticAnalyzer> analyzers, IList<DiagnosticResult> expected)
    {
        var diagnostics = await GetSortedDiagnostics(analyzers).ConfigureAwait(false);
        VerifyDiagnosticResults(diagnostics, analyzers, expected);
    }

    [DebuggerStepThrough]
    private void VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, IList<DiagnosticAnalyzer> analyzers, IList<DiagnosticResult> expectedResults)
    {
        var expectedCount = expectedResults.Count;
        if (DefaultAnalyzerId is not null)
        {
            actualResults = [.. actualResults.Where(diagnostic => diagnostic.Id == DefaultAnalyzerId)];
        }

        var actualCount = actualResults.Count();

        if (expectedCount != actualCount)
        {
            var diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzers, [.. actualResults]) : "    NONE.";

            var annotatedSource = string.Empty;
            var firstDiagnostic = actualResults.FirstOrDefault();
            if (firstDiagnostic?.Location.SourceTree is not null)
            {
                var sourceCode = firstDiagnostic.Location.SourceTree.GetText().ToString();
                annotatedSource = AnnotateSourceCodeWithDiagnostics(sourceCode, actualResults);
            }

            Assert.Fail($"Mismatch between number of diagnostics returned, expected \"{expectedCount.ToString(CultureInfo.InvariantCulture)}\" actual \"{actualCount.ToString(CultureInfo.InvariantCulture)}\"\r\n\r\nDiagnostics:\r\n{diagnosticsOutput}\r\n\r\nSource:\r\n{annotatedSource}");
        }

        for (var i = 0; i < expectedResults.Count; i++)
        {
            var actual = actualResults.ElementAt(i);
            var expected = expectedResults[i];

            if (expected.Line == -1 && expected.Column == -1)
            {
                if (actual.Location != Location.None)
                {
                    var annotatedSource = GetAnnotatedSource(actual.Location, actual);
                    Assert.Fail(string.Format(CultureInfo.InvariantCulture, "Expected:\nA project diagnostic with No location\nActual:\n{0}\r\n\r\nSource:\r\n{1}",
                        FormatDiagnostics(analyzers, actual), annotatedSource));
                }
            }
            else
            {
                VerifyDiagnosticLocation(analyzers, actual, actual.Location, expected.Locations[0]);
                var additionalLocations = actual.AdditionalLocations.ToArray();

                if (additionalLocations.Length != expected.Locations.Count - 1)
                {
                    var annotatedSource = GetAnnotatedSource(actual.Location, actual);
                    Assert.Fail(string.Format(CultureInfo.InvariantCulture,
                            "Expected {0} additional locations but got {1} for Diagnostic:\r\n    {2}\r\n\r\nSource:\r\n{3}",
                            expected.Locations.Count - 1, additionalLocations.Length,
                            FormatDiagnostics(analyzers, actual), annotatedSource));
                }

                for (var j = 0; j < additionalLocations.Length; ++j)
                {
                    VerifyDiagnosticLocation(analyzers, actual, additionalLocations[j], expected.Locations[j + 1]);
                }
            }

            if (expected.Id is not null && !string.Equals(actual.Id, expected.Id, StringComparison.Ordinal))
            {
                var annotatedSource = GetAnnotatedSource(actual.Location, actual);
                Assert.Fail(string.Format(CultureInfo.InvariantCulture, "Expected diagnostic id to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n\r\nSource:\r\n{3}",
                        expected.Id, actual.Id, FormatDiagnostics(analyzers, actual), annotatedSource));
            }

            if (expected.Severity is not null && actual.Severity != expected.Severity)
            {
                var annotatedSource = GetAnnotatedSource(actual.Location, actual);
                Assert.Fail(string.Format(CultureInfo.InvariantCulture, "Expected diagnostic severity to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n\r\nSource:\r\n{3}",
                        expected.Severity, actual.Severity, FormatDiagnostics(analyzers, actual), annotatedSource));
            }

            if (expected.Message is not null && !string.Equals(actual.GetMessage(CultureInfo.InvariantCulture), expected.Message, StringComparison.Ordinal))
            {
                var annotatedSource = GetAnnotatedSource(actual.Location, actual);
                Assert.Fail(string.Format(CultureInfo.InvariantCulture, "Expected diagnostic message to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n\r\nSource:\r\n{3}",
                        expected.Message, actual.GetMessage(CultureInfo.InvariantCulture), FormatDiagnostics(analyzers, actual), annotatedSource));
            }
        }
    }

    private async Task<Diagnostic[]> GetSortedDiagnostics(IList<DiagnosticAnalyzer> analyzers)
    {
        var documents = await GetDocuments().ConfigureAwait(false);
        return await GetSortedDiagnosticsFromDocuments(analyzers, documents, compileSolution: true).ConfigureAwait(false);
    }

    private async Task<Document[]> GetDocuments()
    {
        var project = await CreateProject().ConfigureAwait(false);
        var documents = project.Documents.ToArray();

        var expectedDocuments = ApiReferences.Count + 1;
        if (documents.Length != expectedDocuments)
        {
            throw new InvalidOperationException("Amount of sources did not match amount of Documents created");
        }

        return documents;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    private Task<Project> CreateProject()
    {
        var fileNamePrefix = "Test";
        var fileExt = ".cs";
        var testProjectName = "TestProject";

        var projectId = ProjectId.CreateNewId(debugName: testProjectName);

        switch (TargetFramework)
        {
            case TargetFramework.NetStandard2_0:
                AddNuGetReference("NETStandard.Library", "2.0.3", "build/netstandard2.0/ref/");
                break;

            case TargetFramework.NetStandard2_1:
                AddNuGetReference("NETStandard.Library.Ref", "2.1.0", "ref/netstandard2.1/");
                break;

            case TargetFramework.Net4_8:
                AddNuGetReference("Microsoft.NETFramework.ReferenceAssemblies.net48", "1.0.0", "build/.NETFramework/v4.8/");
                break;

            case TargetFramework.Net5_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "5.0.0", "ref/net5.0/");
                break;

            case TargetFramework.Net6_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "6.0.10", "ref/net6.0/");
                break;

            case TargetFramework.Net7_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "7.0.0", "ref/net7.0/");
                break;

            case TargetFramework.Net8_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "8.0.0", "ref/net8.0/");
                break;

            case TargetFramework.Net9_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "9.0.0", "ref/net9.0/");
                break;

            case TargetFramework.Net10_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "10.0.0", "ref/net10.0/");
                break;

            case TargetFramework.AspNetCore5_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "5.0.0", "ref/net5.0/");
                AddNuGetReference("Microsoft.AspNetCore.App.Ref", "5.0.0", "ref/net5.0/");
                break;

            case TargetFramework.AspNetCore6_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "6.0.10", "ref/net6.0/");
                AddNuGetReference("Microsoft.AspNetCore.App.Ref", "6.0.10", "ref/net6.0/");
                break;

            case TargetFramework.AspNetCore7_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "7.0.0", "ref/net7.0/");
                AddNuGetReference("Microsoft.AspNetCore.App.Ref", "7.0.0", "ref/net7.0/");
                break;

            case TargetFramework.AspNetCore8_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "8.0.0", "ref/net8.0/");
                AddNuGetReference("Microsoft.AspNetCore.App.Ref", "8.0.0", "ref/net8.0/");
                break;

            case TargetFramework.WindowsDesktop5_0:
                AddNuGetReference("Microsoft.WindowsDesktop.App.Ref", "5.0.0", "ref/net5.0/");
                break;
        }


        if (TargetFramework is not TargetFramework.Net7_0 and not TargetFramework.Net8_0 and not TargetFramework.Net9_0 and not TargetFramework.Net10_0)
        {
            AddNuGetReference("System.Collections.Immutable", "1.5.0", "lib/netstandard2.0/");
            AddNuGetReference("System.Numerics.Vectors", "4.5.0", "ref/netstandard2.0/");
        }

        AddNuGetReference("Microsoft.CSharp", "4.7.0", "lib/netstandard2.0/");  // To support dynamic type

        var solution = new AdhocWorkspace()
            .CurrentSolution
            .AddProject(projectId, testProjectName, testProjectName, LanguageNames.CSharp)
            .WithProjectParseOptions(projectId, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion))
            .AddMetadataReferences(projectId, References);

        var count = 0;
        AppendFile(FileName, SourceCode);

        foreach (var source in ApiReferences)
        {
            var newFileName = fileNamePrefix + count.ToString(CultureInfo.InvariantCulture) + fileExt;
            AppendFile(newFileName, source);
        }

        return Task.FromResult(solution.GetProject(projectId)!);

        void AppendFile(string? filename, string content)
        {
            filename ??= fileNamePrefix + count.ToString(CultureInfo.InvariantCulture) + fileExt;
            var documentId = DocumentId.CreateNewId(projectId, debugName: filename);
            solution = solution.AddDocument(documentId, filename, SourceText.From(content), filePath: filename);
            count++;
        }
    }

    private static ReportDiagnostic GetReportDiagnostic(DiagnosticDescriptor descriptor)
    {
        return descriptor.DefaultSeverity switch
        {
            DiagnosticSeverity.Hidden => ReportDiagnostic.Hidden,
            DiagnosticSeverity.Info => ReportDiagnostic.Info,
            DiagnosticSeverity.Warning => ReportDiagnostic.Warn,
            DiagnosticSeverity.Error => ReportDiagnostic.Error,
            _ => ReportDiagnostic.Info, // Ensure the analyzer is enabled for the test
        };
    }

    //[DebuggerStepThrough]
    private async Task<Diagnostic[]> GetSortedDiagnosticsFromDocuments(IList<DiagnosticAnalyzer> analyzers, Document[] documents, bool compileSolution)
    {
        var projects = new HashSet<Project>();
        foreach (var document in documents)
        {
            projects.Add(document.Project);
        }

        var diagnostics = new List<Diagnostic>();
        foreach (var project in projects)
        {
            var options = new CSharpCompilationOptions(OutputKind, allowUnsafe: true, metadataImportOptions: MetadataImportOptions.All);

            // Enable diagnostic
            options = options.WithSpecificDiagnosticOptions(analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics.Select(diag => new KeyValuePair<string, ReportDiagnostic>(diag.Id, GetReportDiagnostic(diag)))));

            var compilation = (await project.GetCompilationAsync().ConfigureAwait(false))!.WithOptions(options);
            if (compileSolution)
            {
                using var ms = new MemoryStream();
                var result = compilation.Emit(ms);
                if (!result.Success)
                {
                    string? sourceCode = null;
                    var document = project.Documents.FirstOrDefault();
                    if (document is not null)
                    {
                        var syntaxRoot = await document.GetSyntaxRootAsync().ConfigureAwait(false);
                        sourceCode = AnnotateSourceCodeWithDiagnostics(syntaxRoot!.ToFullString(), result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
                    }

                    Assert.Fail("The code doesn't compile.\n\n" + sourceCode);
                }
            }

            var additionalFiles = ImmutableArray<AdditionalText>.Empty;
            if (AdditionalFiles is not null)
            {
                additionalFiles = additionalFiles.AddRange(AdditionalFiles.Select(kvp => new InMemoryAdditionalText(kvp.Key, kvp.Value)));
            }

            var analyzerOptionsProvider = new TestAnalyzerConfigOptionsProvider(AnalyzerConfiguration);

            var compilationWithAnalyzers = compilation.WithAnalyzers(
                ImmutableArray.CreateRange(analyzers),
                new AnalyzerOptions(additionalFiles, analyzerOptionsProvider));
            var diags = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None).ConfigureAwait(false);
            foreach (var diag in diags)
            {
#if ROSLYN_3_8
                if (diag.IsSuppressed)
                    continue;
#endif

                if (diag.Location == Location.None || diag.Location.IsInMetadata || !diag.Location.IsInSource)
                {
                    diagnostics.Add(diag);
                }
                else
                {
                    for (var i = 0; i < documents.Length; i++)
                    {
                        var document = documents[i];
                        var tree = await document.GetSyntaxTreeAsync(CancellationToken.None).ConfigureAwait(false);
                        if (tree == diag.Location.SourceTree)
                        {
                            diagnostics.Add(diag);
                        }
                    }
                }
            }
        }

        var results = SortDiagnostics(diagnostics);
        diagnostics.Clear();
        return results;
    }

    private static string FormatDiagnostics(IList<DiagnosticAnalyzer> analyzers, params Diagnostic[] diagnostics)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < diagnostics.Length; ++i)
        {
            builder.Append("// ").Append(diagnostics[i]).AppendLine();

            var analyzerType = analyzers.GetType();
            var rules = analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics);

            foreach (var rule in rules)
            {
                if (rule is not null && string.Equals(rule.Id, diagnostics[i].Id, StringComparison.Ordinal))
                {
                    var location = diagnostics[i].Location;
                    if (location == Location.None)
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, "GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id);
                    }
                    else if (location.SourceTree is null)
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture,
                           "AdditionalFile({0}, {1}, {2}.{3})",
                           location.GetLineSpan().StartLinePosition.Line + 1,
                           location.GetLineSpan().StartLinePosition.Character + 1,
                           analyzerType.Name,
                           rule.Id);
                    }
                    else
                    {
                        Assert.True(location.IsInSource, $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostics[i]}\r\n");

                        var resultMethodName = diagnostics[i].Location.SourceTree!.FilePath.EndsWith(".cs", StringComparison.Ordinal) ? "GetCSharpResultAt" : "GetBasicResultAt";
                        var linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;

                        builder.AppendFormat(
                            CultureInfo.InvariantCulture,
                            "{0}({1}, {2}, {3}.{4})",
                            resultMethodName,
                            linePosition.Line + 1,
                            linePosition.Character + 1,
                            analyzerType.Name,
                            rule.Id);
                    }

                    if (i != diagnostics.Length - 1)
                    {
                        builder.Append(',');
                    }

                    builder.AppendLine();
                    break;
                }
            }
        }

        return builder.ToString();
    }

    private static Diagnostic[] SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        return [.. diagnostics.OrderBy(d => d.Location.SourceSpan.Start)];
    }

    [DebuggerStepThrough]
    private static void VerifyDiagnosticLocation(IList<DiagnosticAnalyzer> analyzers, Diagnostic diagnostic, Location actual, in DiagnosticResultLocation expected)
    {
        var actualSpan = actual.GetLineSpan();

        Assert.True(string.Equals(actualSpan.Path, expected.Path, StringComparison.Ordinal) || (actualSpan.Path is not null && actualSpan.Path.StartsWith("Test", StringComparison.Ordinal) && expected.Path.EndsWith(".cs", StringComparison.Ordinal)),
            string.Format(CultureInfo.InvariantCulture, "Expected diagnostic to be in file \"{0}\" was actually in file \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                expected.Path, actualSpan.Path, FormatDiagnostics(analyzers, diagnostic)));

        var actualLinePosition = actualSpan.StartLinePosition;

        // Only check line position if there is an actual line in the real diagnostic
        if (actualLinePosition.Line > 0)
        {
            if (actualLinePosition.Line + 1 != expected.LineStart)
            {
                var annotatedSource = GetAnnotatedSource(actual, diagnostic);
                Assert.Fail(string.Format(CultureInfo.InvariantCulture, "Expected diagnostic to be on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n\r\nSource:\r\n{3}",
                        expected.LineStart, actualLinePosition.Line + 1, FormatDiagnostics(analyzers, diagnostic), annotatedSource));
            }
        }

        // Only check column position if there is an actual column position in the real diagnostic
        if (actualLinePosition.Character > 0)
        {
            if (actualLinePosition.Character + 1 != expected.ColumnStart)
            {
                var annotatedSource = GetAnnotatedSource(actual, diagnostic);
                Assert.Fail(string.Format(CultureInfo.InvariantCulture, "Expected diagnostic to start at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n\r\nSource:\r\n{3}",
                        expected.ColumnStart, actualLinePosition.Character + 1, FormatDiagnostics(analyzers, diagnostic), annotatedSource));
            }
        }

        if (expected.IsSpan)
        {
            actualLinePosition = actualSpan.EndLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualLinePosition.Line > 0)
            {
                if (actualLinePosition.Line + 1 != expected.LineEnd)
                {
                    var annotatedSource = GetAnnotatedSource(actual, diagnostic);
                    Assert.Fail(string.Format(CultureInfo.InvariantCulture, "Expected diagnostic to end on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n\r\nSource:\r\n{3}",
                            expected.LineEnd, actualLinePosition.Line + 1, FormatDiagnostics(analyzers, diagnostic), annotatedSource));
                }
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if (actualLinePosition.Character > 0)
            {
                if (actualLinePosition.Character + 1 != expected.ColumnEnd)
                {
                    var annotatedSource = GetAnnotatedSource(actual, diagnostic);
                    Assert.Fail(string.Format(CultureInfo.InvariantCulture, "Expected diagnostic to end at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n\r\nSource:\r\n{3}",
                            expected.ColumnEnd, actualLinePosition.Character + 1, FormatDiagnostics(analyzers, diagnostic), annotatedSource));
                }
            }
        }
    }

    private static string GetAnnotatedSource(Location location, Diagnostic diagnostic)
    {
        if (location.SourceTree is null)
            return string.Empty;

        var sourceCode = location.SourceTree.GetText().ToString();
        return AnnotateSourceCodeWithDiagnostics(sourceCode, [diagnostic]);
    }

    private static string GetTextSpanContent(Location location)
    {
        if (location.SourceTree is null)
            return string.Empty;

        var sourceText = location.SourceTree.GetText();
        var span = location.SourceSpan;

        if (span.Start < 0 || span.End > sourceText.Length)
            return string.Empty;

        return sourceText.ToString(span);
    }

    private static async Task<IEnumerable<Diagnostic>> GetCompilerDiagnostics(Document document)
    {
        var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
        return semanticModel!.GetDiagnostics();
    }

    private async Task VerifyFix(IList<DiagnosticAnalyzer> analyzers, CodeFixProvider codeFixProvider, string newSource, int? codeFixIndex)
    {
        var project = await CreateProject().ConfigureAwait(false);
        var document = project.Documents.First();
        var analyzerDiagnostics = await GetSortedDiagnosticsFromDocuments(analyzers, [document], compileSolution: false).ConfigureAwait(false);
        var compilerDiagnostics = await GetCompilerDiagnostics(document).ConfigureAwait(false);

        // Assert fixer is value
        foreach (var diagnostic in analyzerDiagnostics)
        {
            if (!codeFixProvider.FixableDiagnosticIds.Any(id => string.Equals(diagnostic.Id, id, StringComparison.Ordinal)))
            {
                Assert.Fail($"The CodeFixProvider is not valid for the DiagnosticAnalyzer. DiagnosticId: {diagnostic.Id}, Supported diagnostics: {string.Join(',', codeFixProvider.FixableDiagnosticIds)}");
            }
        }

        if (UseBatchFixer)
        {
            var diagnostic = analyzerDiagnostics[0];

            var actions = new List<CodeAction>();
            var context = new CodeFixContext(document, diagnostic, (a, _) => actions.Add(a), CancellationToken.None);
            await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            if (actions.Count > 0)
            {
                var action = actions[codeFixIndex ?? 0];
                var fixAllContext = new FixAllContext(document, codeFixProvider, FixAllScope.Document, action.EquivalenceKey, analyzerDiagnostics.Select(d => d.Id).Distinct(StringComparer.Ordinal), new CustomDiagnosticProvider(analyzerDiagnostics), CancellationToken.None);
                var fixes = await codeFixProvider.GetFixAllProvider()!.GetFixAsync(fixAllContext).ConfigureAwait(false);

                document = await ApplyFix(document, fixes!, mustCompile: IsValidFixCode).ConfigureAwait(false);
            }
        }
        else
        {
            for (var i = 0; i < analyzerDiagnostics.Length; ++i)
            {
                var diagnostic = analyzerDiagnostics[0];
                var actions = new List<CodeAction>();
                var context = new CodeFixContext(document, diagnostic, (a, _) => actions.Add(a), CancellationToken.None);
                await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

                if (actions.Count == 0)
                    break;

                if (codeFixIndex is not null)
                {
                    document = await ApplyFix(document, actions[(int)codeFixIndex], mustCompile: IsValidFixCode).ConfigureAwait(false);
                    break;
                }

                document = await ApplyFix(document, actions[0], mustCompile: IsValidFixCode).ConfigureAwait(false);
                if (document is null)
                    break;
                analyzerDiagnostics = await GetSortedDiagnosticsFromDocuments(analyzers, [document], compileSolution: false).ConfigureAwait(false);
            }
        }

        // after applying all of the code fixes, compare the resulting string to the inputted one
        var actual = document is not null ? await GetStringFromDocument(document).ConfigureAwait(false) : string.Empty;
        Assert.Equal(newSource, actual, ignoreLineEndingDifferences: true);
    }

    private async Task<Document?> ApplyFix(Document document, CodeAction codeAction, bool mustCompile)
    {
        var operations = await codeAction.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
        var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;

        if (mustCompile)
        {
            var options = new CSharpCompilationOptions(OutputKind, allowUnsafe: true, metadataImportOptions: MetadataImportOptions.All);
            var project = solution.Projects.Single();
            var compilation = (await project.GetCompilationAsync().ConfigureAwait(false))!.WithOptions(options);

            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);
            if (!result.Success)
            {
                string? sourceCode = null;
                var firstDocument = project.Documents.FirstOrDefault();
                if (firstDocument is not null)
                {
                    sourceCode = (await firstDocument.GetSyntaxRootAsync().ConfigureAwait(false))!.ToFullString();
                }

                Assert.Fail("The fixed code doesn't compile. " + string.Join(Environment.NewLine, result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)) + Environment.NewLine + sourceCode);
            }
        }

        return solution.GetDocument(document.Id);
    }

    private static async Task<string> GetStringFromDocument(Document document)
    {
        var simplifiedDoc = await Simplifier.ReduceAsync(document, Simplifier.Annotation).ConfigureAwait(false);
        var root = await simplifiedDoc.GetSyntaxRootAsync().ConfigureAwait(false);
        root = Formatter.Format(root!, Formatter.Annotation, simplifiedDoc.Project.Solution.Workspace);
        return root.GetText().ToString();
    }

    private static string AnnotateSourceCodeWithDiagnostics(string sourceCode, IEnumerable<Diagnostic> diagnostics)
    {
        var lines = sourceCode.SplitLines();
        var result = new StringBuilder();
        var diagnosticsByLine = diagnostics
            .Where(d => d.Location.IsInSource)
            .GroupBy(d => d.Location.GetLineSpan().StartLinePosition.Line)
            .ToDictionary(g => g.Key, g => g.ToList());

        var lineIndex = 0;
        foreach (var (line, separator) in lines)
        {
            result.Append(line).Append(separator);

            if (diagnosticsByLine.TryGetValue(lineIndex, out var lineDiagnostics))
            {
                foreach (var diagnostic in lineDiagnostics.OrderBy(d => d.Location.GetLineSpan().StartLinePosition.Character))
                {
                    var lineSpan = diagnostic.Location.GetLineSpan();
                    var startColumn = lineSpan.StartLinePosition.Character;
                    var endColumn = lineSpan.StartLinePosition.Line == lineSpan.EndLinePosition.Line
                        ? lineSpan.EndLinePosition.Character
                        : line.TrimEnd('\r').Length;

                    var underlineLength = Math.Max(1, endColumn - startColumn);
                    var annotation = new string(' ', startColumn) + new string('^', underlineLength) + " " + diagnostic.Id + ": " + diagnostic.GetMessage(CultureInfo.InvariantCulture);
                    result.AppendLine(annotation);
                }
            }

            lineIndex++;
        }

        return result.ToString();
    }

    private sealed class CustomDiagnosticProvider(Diagnostic[] diagnostics) : FixAllContext.DiagnosticProvider
    {
        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return GetProjectDiagnosticsAsync(project, cancellationToken);
        }

        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var documentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return diagnostics.Where(diagnostic => documentRoot == diagnostic.Location.SourceTree!.GetRoot(cancellationToken));
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return Task.FromResult(project.Documents.SelectMany(doc => GetDocumentDiagnosticsAsync(doc, cancellationToken).Result));
        }
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            Content = content;
        }

        public override string Path { get; }
        public string Content { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(Content, Encoding.UTF8);
        }
    }
}


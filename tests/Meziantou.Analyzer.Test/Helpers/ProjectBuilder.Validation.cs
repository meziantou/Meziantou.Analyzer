using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace TestHelper
{
    public sealed partial class ProjectBuilder
    {
        public async Task ValidateAsync()
        {
            if (DiagnosticAnalyzer == null)
            {
                Assert.True(false, "DiagnosticAnalyzer is not configured");
            }

            if (ExpectedFixedCode != null && CodeFixProvider == null)
            {
                Assert.True(false, "CodeFixProvider is not configured");
            }

            if (ExpectedDiagnosticResults == null)
            {
                Assert.True(false, "ExpectedDiagnostic is not configured");
            }

            await VerifyDiagnostic(ExpectedDiagnosticResults).ConfigureAwait(false);

            if (ExpectedFixedCode != null)
            {
                await VerifyFix(DiagnosticAnalyzer, CodeFixProvider, ExpectedFixedCode, CodeFixIndex).ConfigureAwait(false);
            }
        }

        [DebuggerStepThrough]
        private Task VerifyDiagnostic(IList<DiagnosticResult> expected)
        {
            return VerifyDiagnostics(DiagnosticAnalyzer, expected);
        }

        [DebuggerStepThrough]
        private async Task VerifyDiagnostics(DiagnosticAnalyzer analyzer, IList<DiagnosticResult> expected)
        {
            var diagnostics = await GetSortedDiagnostics(analyzer).ConfigureAwait(false);
            VerifyDiagnosticResults(diagnostics, analyzer, expected);
        }

        [DebuggerStepThrough]
        private static void VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, IList<DiagnosticResult> expectedResults)
        {
            var expectedCount = expectedResults.Count;
            var actualCount = actualResults.Count();

            if (expectedCount != actualCount)
            {
                var diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzer, actualResults.ToArray()) : "    NONE.";

                Assert.True(false, $"Mismatch between number of diagnostics returned, expected \"{expectedCount}\" actual \"{actualCount}\"\r\n\r\nDiagnostics:\r\n{diagnosticsOutput}\r\n");
            }

            for (var i = 0; i < expectedResults.Count; i++)
            {
                var actual = actualResults.ElementAt(i);
                var expected = expectedResults[i];

                if (expected.Line == -1 && expected.Column == -1)
                {
                    if (actual.Location != Location.None)
                    {
                        Assert.True(false,
                            string.Format("Expected:\nA project diagnostic with No location\nActual:\n{0}",
                            FormatDiagnostics(analyzer, actual)));
                    }
                }
                else
                {
                    VerifyDiagnosticLocation(analyzer, actual, actual.Location, expected.Locations[0]);
                    var additionalLocations = actual.AdditionalLocations.ToArray();

                    if (additionalLocations.Length != expected.Locations.Length - 1)
                    {
                        Assert.True(false,
                            string.Format("Expected {0} additional locations but got {1} for Diagnostic:\r\n    {2}\r\n",
                                expected.Locations.Length - 1, additionalLocations.Length,
                                FormatDiagnostics(analyzer, actual)));
                    }

                    for (var j = 0; j < additionalLocations.Length; ++j)
                    {
                        VerifyDiagnosticLocation(analyzer, actual, additionalLocations[j], expected.Locations[j + 1]);
                    }
                }

                if (expected.Id != null && !string.Equals(actual.Id, expected.Id, StringComparison.Ordinal))
                {
                    Assert.True(false,
                        string.Format("Expected diagnostic id to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Id, actual.Id, FormatDiagnostics(analyzer, actual)));
                }

                if (expected.Severity != null && actual.Severity != expected.Severity)
                {
                    Assert.True(false,
                        string.Format("Expected diagnostic severity to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Severity, actual.Severity, FormatDiagnostics(analyzer, actual)));
                }

                if (expected.Message != null && !string.Equals(actual.GetMessage(), expected.Message, StringComparison.Ordinal))
                {
                    Assert.True(false,
                        string.Format("Expected diagnostic message to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Message, actual.GetMessage(), FormatDiagnostics(analyzer, actual)));
                }
            }
        }

        private async Task<Diagnostic[]> GetSortedDiagnostics(DiagnosticAnalyzer analyzer)
        {
            var documents = await GetDocuments().ConfigureAwait(false);
            return await GetSortedDiagnosticsFromDocuments(analyzer, documents, compileSolution: true).ConfigureAwait(false);
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
                case TargetFramework.Net48:
                    AddNuGetReference("Microsoft.NETFramework.ReferenceAssemblies.net48", "1.0.0", "build/.NETFramework/v4.8/");
                    break;
            }

            AddNuGetReference("System.Collections.Immutable", "1.5.0", "lib/netstandard2.0/");
            AddNuGetReference("System.Numerics.Vectors", "4.5.0", "ref/netstandard2.0/");

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, testProjectName, testProjectName, LanguageNames.CSharp)
                .WithProjectParseOptions(projectId, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion))
                .AddMetadataReferences(projectId, References);

            var count = 0;
            AppendFile(FileName, SourceCode);

            foreach (var source in ApiReferences)
            {
                var newFileName = fileNamePrefix + count + fileExt;
                AppendFile(newFileName, source);
            }

            return Task.FromResult(solution.GetProject(projectId));

            void AppendFile(string filename, string content)
            {
                filename ??= fileNamePrefix + count + fileExt;
                var documentId = DocumentId.CreateNewId(projectId, debugName: filename);
                solution = solution.AddDocument(documentId, filename, SourceText.From(content));
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

        [DebuggerStepThrough]
        private async Task<Diagnostic[]> GetSortedDiagnosticsFromDocuments(DiagnosticAnalyzer analyzer, Document[] documents, bool compileSolution)
        {
            var projects = new HashSet<Project>();
            foreach (var document in documents)
            {
                projects.Add(document.Project);
            }

            var diagnostics = new List<Diagnostic>();
            foreach (var project in projects)
            {
                var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

                // Enable diagnostic
                options = options.WithSpecificDiagnosticOptions(analyzer.SupportedDiagnostics.Select(diag => new KeyValuePair<string, ReportDiagnostic>(diag.Id, GetReportDiagnostic(diag))));

                var compilation = (await project.GetCompilationAsync().ConfigureAwait(false)).WithOptions(options);
                if (compileSolution)
                {
                    using var ms = new MemoryStream();
                    var result = compilation.Emit(ms);
                    if (!result.Success)
                    {
                        string sourceCode = null;
                        var document = project.Documents.FirstOrDefault();
                        if (document != null)
                        {
                            sourceCode = (await document.GetSyntaxRootAsync().ConfigureAwait(false)).ToFullString();
                        }

                        Assert.True(false, "The code doesn't compile. " + string.Join(Environment.NewLine, result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)) + Environment.NewLine + sourceCode);
                    }
                }

                var additionalFiles = ImmutableArray<AdditionalText>.Empty;
                if (EditorConfig != null)
                {
                    additionalFiles = additionalFiles.Add(new TestAdditionalFile(".editorconfig", EditorConfig));
                }

                var compilationWithAnalyzers = compilation.WithAnalyzers(
                    ImmutableArray.Create(analyzer),
                    new AnalyzerOptions(additionalFiles));
                var diags = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(compilationWithAnalyzers.CancellationToken).ConfigureAwait(false);
                foreach (var diag in diags)
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                    {
                        diagnostics.Add(diag);
                    }
                    else
                    {
                        for (var i = 0; i < documents.Length; i++)
                        {
                            var document = documents[i];
                            var tree = await document.GetSyntaxTreeAsync(compilationWithAnalyzers.CancellationToken).ConfigureAwait(false);
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

        private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < diagnostics.Length; ++i)
            {
                builder.Append("// ").Append(diagnostics[i]).AppendLine();

                var analyzerType = analyzer.GetType();
                var rules = analyzer.SupportedDiagnostics;

                foreach (var rule in rules)
                {
                    if (rule != null && string.Equals(rule.Id, diagnostics[i].Id, StringComparison.Ordinal))
                    {
                        var location = diagnostics[i].Location;
                        if (location == Location.None)
                        {
                            builder.AppendFormat(CultureInfo.InvariantCulture, "GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id);
                        }
                        else
                        {
                            Assert.True(location.IsInSource, $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostics[i]}\r\n");

                            var resultMethodName = diagnostics[i].Location.SourceTree.FilePath.EndsWith(".cs", StringComparison.Ordinal) ? "GetCSharpResultAt" : "GetBasicResultAt";
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
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        [DebuggerStepThrough]
        private static void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, in DiagnosticResultLocation expected)
        {
            var actualSpan = actual.GetLineSpan();

            Assert.True(string.Equals(actualSpan.Path, expected.Path, StringComparison.Ordinal) || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Path.Contains("Test.")),
                string.Format("Expected diagnostic to be in file \"{0}\" was actually in file \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                    expected.Path, actualSpan.Path, FormatDiagnostics(analyzer, diagnostic)));

            var actualLinePosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualLinePosition.Line > 0)
            {
                if (actualLinePosition.Line + 1 != expected.LineStart)
                {
                    Assert.True(false,
                        string.Format("Expected diagnostic to be on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.LineStart, actualLinePosition.Line + 1, FormatDiagnostics(analyzer, diagnostic)));
                }
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if (actualLinePosition.Character > 0)
            {
                if (actualLinePosition.Character + 1 != expected.ColumnStart)
                {
                    Assert.True(false,
                        string.Format("Expected diagnostic to start at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.ColumnStart, actualLinePosition.Character + 1, FormatDiagnostics(analyzer, diagnostic)));
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
                        Assert.True(false,
                            string.Format("Expected diagnostic to end on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                expected.LineStart, actualLinePosition.Line + 1, FormatDiagnostics(analyzer, diagnostic)));
                    }
                }

                // Only check column position if there is an actual column position in the real diagnostic
                if (actualLinePosition.Character > 0)
                {
                    if (actualLinePosition.Character + 1 != expected.ColumnEnd)
                    {
                        Assert.True(false,
                            string.Format("Expected diagnostic to end at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                expected.ColumnStart, actualLinePosition.Character + 1, FormatDiagnostics(analyzer, diagnostic)));
                    }
                }
            }
        }

        private async static Task<IEnumerable<Diagnostic>> GetCompilerDiagnostics(Document document)
        {
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
            return semanticModel.GetDiagnostics();
        }

        private async Task VerifyFix(DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string newSource, int? codeFixIndex)
        {
            var project = await CreateProject().ConfigureAwait(false);
            var document = project.Documents.First();
            var analyzerDiagnostics = await GetSortedDiagnosticsFromDocuments(analyzer, new[] { document }, compileSolution: false).ConfigureAwait(false);
            var compilerDiagnostics = await GetCompilerDiagnostics(document).ConfigureAwait(false);

            // Assert fixer is value
            foreach (var diagnostic in analyzerDiagnostics)
            {
                if (!codeFixProvider.FixableDiagnosticIds.Any(id => string.Equals(diagnostic.Id, id, StringComparison.Ordinal)))
                {
                    Assert.True(false, "The CodeFixProvider is not valid for the DiagnosticAnalyzer");
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
                    var fixes = await codeFixProvider.GetFixAllProvider().GetFixAsync(fixAllContext).ConfigureAwait(false);

                    document = await ApplyFix(document, fixes).ConfigureAwait(false);
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

                    if (codeFixIndex != null)
                    {
                        document = await ApplyFix(document, actions[(int)codeFixIndex]).ConfigureAwait(false);
                        break;
                    }

                    document = await ApplyFix(document, actions[0]).ConfigureAwait(false);
                    analyzerDiagnostics = await GetSortedDiagnosticsFromDocuments(analyzer, new[] { document }, compileSolution: false).ConfigureAwait(false);
                }
            }

            //after applying all of the code fixes, compare the resulting string to the inputted one
            var actual = await GetStringFromDocument(document).ConfigureAwait(false);
            Assert.Equal(newSource, actual);
        }

        private static async Task<Document> ApplyFix(Document document, CodeAction codeAction)
        {
            var operations = await codeAction.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
            var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            return solution.GetDocument(document.Id);
        }

        private static async Task<string> GetStringFromDocument(Document document)
        {
            var simplifiedDoc = await Simplifier.ReduceAsync(document, Simplifier.Annotation).ConfigureAwait(false);
            var root = await simplifiedDoc.GetSyntaxRootAsync().ConfigureAwait(false);
            root = Formatter.Format(root, Formatter.Annotation, simplifiedDoc.Project.Solution.Workspace);
            return root.GetText().ToString();
        }

        private sealed class CustomDiagnosticProvider : FixAllContext.DiagnosticProvider
        {
            private readonly Diagnostic[] _diagnostics;

            public CustomDiagnosticProvider(Diagnostic[] diagnostics)
            {
                _diagnostics = diagnostics;
            }

            public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            {
                // TODO I'm not sure of this one
                return GetProjectDiagnosticsAsync(project, cancellationToken);
            }

            public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
            {
                var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
                return _diagnostics.Where(d => root == d.Location.SourceTree.GetRoot());
            }

            public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            {
                // TODO using project.Documents
                return Task.FromResult(Enumerable.Empty<Diagnostic>());
            }
        }
    }
}


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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestHelper
{
    public partial class ProjectBuilder
    {
        public async Task ValidateAsync()
        {
            if (DiagnosticAnalyzer == null)
            {
                Assert.Fail("DiagnosticAnalyzer is not configured");
            }

            if (ExpectedFixedCode != null && CodeFixProvider == null)
            {
                Assert.Fail("CodeFixProvider is not configured");
            }

            if (ExpectedDiagnosticResults == null)
            {
                Assert.Fail("ExpectedDiagnostic is not configured");
            }

            await VerifyDiagnostic(ExpectedDiagnosticResults).ConfigureAwait(false);

            if (ExpectedFixedCode != null)
            {
                await VerifyFix(DiagnosticAnalyzer, CodeFixProvider, ExpectedFixedCode, null, allowNewCompilerDiagnostics: AllowNewCompilerDiagnostics).ConfigureAwait(false);
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

                Assert.Fail("Mismatch between number of diagnostics returned, expected \"{0}\" actual \"{1}\"\r\n\r\nDiagnostics:\r\n{2}\r\n", expectedCount, actualCount, diagnosticsOutput);
            }

            for (var i = 0; i < expectedResults.Count; i++)
            {
                var actual = actualResults.ElementAt(i);
                var expected = expectedResults[i];

                if (expected.Line == -1 && expected.Column == -1)
                {
                    if (actual.Location != Location.None)
                    {
                        Assert.IsTrue(false,
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
                        Assert.IsTrue(false,
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
                    Assert.IsTrue(false,
                        string.Format("Expected diagnostic id to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Id, actual.Id, FormatDiagnostics(analyzer, actual)));
                }

                if (expected.Severity != null && actual.Severity != expected.Severity)
                {
                    Assert.IsTrue(false,
                        string.Format("Expected diagnostic severity to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Severity, actual.Severity, FormatDiagnostics(analyzer, actual)));
                }

                if (expected.Message != null && !string.Equals(actual.GetMessage(), expected.Message, StringComparison.Ordinal))
                {
                    Assert.IsTrue(false,
                        string.Format("Expected diagnostic message to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Message, actual.GetMessage(), FormatDiagnostics(analyzer, actual)));
                }
            }
        }

        private Task<Diagnostic[]> GetSortedDiagnostics(DiagnosticAnalyzer analyzer)
        {
            return GetSortedDiagnosticsFromDocuments(analyzer, GetDocuments(), compileSolution: true);
        }

        private Document[] GetDocuments()
        {
            var project = CreateProject();
            var documents = project.Documents.ToArray();

            if (ApiReferences.Count + 1 != documents.Length)
            {
                throw new InvalidOperationException("Amount of sources did not match amount of Documents created");
            }

            return documents;
        }

        private Project CreateProject()
        {
            var fileNamePrefix = "Test";
            var fileExt = ".cs";
            var testProjectName = "TestProject";

            var projectId = ProjectId.CreateNewId(debugName: testProjectName);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, testProjectName, testProjectName, LanguageNames.CSharp)
                .WithProjectParseOptions(projectId, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion))
                .AddMetadataReferences(projectId, References);

            var count = 0;
            foreach (var source in new[] { SourceCode }.Concat(ApiReferences))
            {
                var newFileName = fileNamePrefix + count + fileExt;
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                count++;
            }
            return solution.GetProject(projectId);
        }

        [DebuggerStepThrough]
        private static async Task<Diagnostic[]> GetSortedDiagnosticsFromDocuments(DiagnosticAnalyzer analyzer, Document[] documents, bool compileSolution)
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
                //options.WithSpecificDiagnosticOptions(ImmutableDictionary.Create<string, ReportDiagnostic>()
                //    .Add("", ReportDiagnostic.Info));
                //project.AddAdditionalDocument("")
                var compilation = (await project.GetCompilationAsync().ConfigureAwait(false)).WithOptions(options);

                if (compileSolution)
                {
                    using (var ms = new MemoryStream())
                    {
                        var result = compilation.Emit(ms);
                        if (!result.Success)
                        {
                            string sourceCode = null;
                            var document = project.Documents.FirstOrDefault();
                            if (document != null)
                            {
                                sourceCode = (await document.GetSyntaxRootAsync().ConfigureAwait(false)).ToFullString();
                            }

                            Assert.Fail("The code doesn't compile. " + string.Join(Environment.NewLine, result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)) + Environment.NewLine + sourceCode);
                        }
                    }
                }

                var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
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
                            Assert.IsTrue(location.IsInSource, $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostics[i]}\r\n");

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

            Assert.IsTrue(string.Equals(actualSpan.Path, expected.Path, StringComparison.Ordinal) || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Path.Contains("Test.")),
                string.Format("Expected diagnostic to be in file \"{0}\" was actually in file \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                    expected.Path, actualSpan.Path, FormatDiagnostics(analyzer, diagnostic)));

            var actualLinePosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualLinePosition.Line > 0)
            {
                if (actualLinePosition.Line + 1 != expected.Line)
                {
                    Assert.IsTrue(false,
                        string.Format("Expected diagnostic to be on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Line, actualLinePosition.Line + 1, FormatDiagnostics(analyzer, diagnostic)));
                }
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if (actualLinePosition.Character > 0)
            {
                if (actualLinePosition.Character + 1 != expected.Column)
                {
                    Assert.IsTrue(false,
                        string.Format("Expected diagnostic to start at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Column, actualLinePosition.Character + 1, FormatDiagnostics(analyzer, diagnostic)));
                }
            }
        }

        private async static Task<IEnumerable<Diagnostic>> GetCompilerDiagnostics(Document document)
        {
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
            return semanticModel.GetDiagnostics();
        }

        private async Task VerifyFix(DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string newSource, int? codeFixIndex, bool allowNewCompilerDiagnostics)
        {
            if (!codeFixProvider.FixableDiagnosticIds.Any(id => analyzer.SupportedDiagnostics.Any(descriptor => string.Equals(descriptor.Id, id, StringComparison.Ordinal))))
            {
                Assert.Fail("The CodeFixProvider is not valid for the DiagnosticAnalyzer");
            }

            var document = CreateProject().Documents.First();
            var analyzerDiagnostics = await GetSortedDiagnosticsFromDocuments(analyzer, new[] { document }, compileSolution: false).ConfigureAwait(false);
            var compilerDiagnostics = await GetCompilerDiagnostics(document).ConfigureAwait(false);

            for (var i = 0; i < analyzerDiagnostics.Length; ++i)
            {
                var actions = new List<CodeAction>();
                var context = new CodeFixContext(document, analyzerDiagnostics[0], (a, _) => actions.Add(a), CancellationToken.None);
                codeFixProvider.RegisterCodeFixesAsync(context).Wait(context.CancellationToken);

                if (actions.Count == 0)
                {
                    break;
                }

                if (codeFixIndex != null)
                {
                    document = await ApplyFix(document, actions[(int)codeFixIndex]).ConfigureAwait(false);
                    break;
                }

                document = await ApplyFix(document, actions[0]).ConfigureAwait(false);
                analyzerDiagnostics = await GetSortedDiagnosticsFromDocuments(analyzer, new[] { document }, compileSolution: false).ConfigureAwait(false);

                var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnostics(document).ConfigureAwait(false));

                //check if applying the code fix introduced any new compiler diagnostics
                if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
                {
                    // Format and get the compiler diagnostics again so that the locations make sense in the output
                    document = document.WithSyntaxRoot(Formatter.Format(await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false), Formatter.Annotation, document.Project.Solution.Workspace, cancellationToken: context.CancellationToken));
                    newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnostics(document).ConfigureAwait(false));

                    Assert.IsTrue(false,
                        string.Format("Fix introduced new compiler diagnostics:\r\n{0}\r\n\r\nNew document:\r\n{1}\r\n",
                            string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString())),
                           (await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false)).ToFullString()));
                }

                //check if there are analyzer diagnostics left after the code fix
                if (analyzerDiagnostics.Length == 0)
                {
                    break;
                }
            }

            //after applying all of the code fixes, compare the resulting string to the inputted one
            var actual = await GetStringFromDocument(document).ConfigureAwait(false);
            Assert.AreEqual(newSource, actual);
        }

        private static async Task<Document> ApplyFix(Document document, CodeAction codeAction)
        {
            var operations = await codeAction.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
            var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            return solution.GetDocument(document.Id);
        }

        private static IEnumerable<Diagnostic> GetNewDiagnostics(IEnumerable<Diagnostic> diagnostics, IEnumerable<Diagnostic> newDiagnostics)
        {
            var oldArray = diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
            var newArray = newDiagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();

            int oldIndex = 0;
            int newIndex = 0;

            while (newIndex < newArray.Length)
            {
                if (oldIndex < oldArray.Length && string.Equals(oldArray[oldIndex].Id, newArray[newIndex].Id, StringComparison.Ordinal))
                {
                    ++oldIndex;
                    ++newIndex;
                }
                else
                {
                    yield return newArray[newIndex++];
                }
            }
        }

        private static async Task<string> GetStringFromDocument(Document document)
        {
            var simplifiedDoc = await Simplifier.ReduceAsync(document, Simplifier.Annotation).ConfigureAwait(false);
            var root = await simplifiedDoc.GetSyntaxRootAsync().ConfigureAwait(false);
            root = Formatter.Format(root, Formatter.Annotation, simplifiedDoc.Project.Solution.Workspace);
            return root.GetText().ToString();
        }
    }
}


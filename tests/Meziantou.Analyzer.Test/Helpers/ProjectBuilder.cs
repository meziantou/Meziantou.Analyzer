using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestHelper
{
    public sealed partial class ProjectBuilder
    {
        private int _diagnosticMessageIndex = 0;

        public ProjectBuilder()
        {
            var list = new List<MetadataReference>();
            list.AddRange(Initialize.NetStandard2_0.Select(file => MetadataReference.CreateFromFile(file)));
            list.AddRange(Initialize.System_Collections_Immutable.Select(file => MetadataReference.CreateFromFile(file)));

            References = list;
        }

        public string FileName { get; private set; }
        public string SourceCode { get; private set; } = "";
        public string EditorConfig { get; private set; }
        public bool IsValidCode { get; private set; } = true;
        public LanguageVersion LanguageVersion { get; private set; } = LanguageVersion.Latest;
        public IList<MetadataReference> References { get; }
        public IList<string> ApiReferences { get; } = new List<string>();
        public DiagnosticAnalyzer DiagnosticAnalyzer { get; private set; }
        public CodeFixProvider CodeFixProvider { get; private set; }
        public IList<DiagnosticResult> ExpectedDiagnosticResults { get; } = new List<DiagnosticResult>();
        public string ExpectedFixedCode { get; private set; }
        public int? CodeFixIndex { get; private set; }
        public string DefaultAnalyzerId { get; set; }
        public string DefaultAnalyzerMessage { get; set; }

        private ProjectBuilder AddApiReference(string name)
        {
            using (var stream = typeof(ProjectBuilder).Assembly.GetManifestResourceStream("Meziantou.Analyzer.Test.References." + name + ".txt"))
            {
                if (stream == null)
                {
                    var names = typeof(ProjectBuilder).Assembly.GetManifestResourceNames();
                    throw new Exception("File not found. Available values:" + Environment.NewLine + string.Join(Environment.NewLine, names));
                }

                using (var sr = new StreamReader(stream))
                {
                    var content = sr.ReadToEnd();
                    ApiReferences.Add(content);
                }
            }

            return this;
        }

        public ProjectBuilder AddWpfApi() => AddApiReference("System.Windows.Window");

        public ProjectBuilder AddMSTestApi() => AddApiReference("MSTest");

        public ProjectBuilder AddNUnitApi() => AddApiReference("NUnit");

        public ProjectBuilder AddXUnitApi() => AddApiReference("XUnit");

        public ProjectBuilder AddMicrosoftAspNetCoreApi() => AddApiReference("Microsoft.AspNetCore");

        public ProjectBuilder WithSourceCode(string sourceCode)
        {
            return WithSourceCode(fileName: null, sourceCode);
        }

        public ProjectBuilder WithSourceCode(string fileName, string sourceCode)
        {
            FileName = fileName;
            ParseSourceCode(sourceCode);
            return this;
        }

        private void ParseSourceCode(string sourceCode)
        {
            const string Pattern = "[|]";
            SourceCode = sourceCode.Replace(Pattern, "");

            using (var sr = new StringReader(sourceCode))
            {
                int lineIndex = 0;
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    lineIndex++;

                    int startIndex = 0;
                    int matchCount = 0;
                    while (true)
                    {
                        var index = line.IndexOf(Pattern, startIndex, StringComparison.Ordinal);
                        if (index < 0)
                            break;

                        ShouldReportDiagnostic(lineIndex, index + 1 - (matchCount * Pattern.Length));
                        startIndex = index + 1;
                        matchCount++;
                    }
                }
            }
        }

        public ProjectBuilder WithEditorConfig(string editorConfig)
        {
            EditorConfig = editorConfig;
            return this;
        }

        public ProjectBuilder WithLanguageVersion(LanguageVersion languageVersion)
        {
            LanguageVersion = languageVersion;
            return this;
        }

        public ProjectBuilder WithCompilation()
        {
            IsValidCode = true;
            return this;
        }

        public ProjectBuilder WithNoCompilation()
        {
            IsValidCode = false;
            return this;
        }

        public ProjectBuilder WithAnalyzer(DiagnosticAnalyzer diagnosticAnalyzer, string id = null, string message = null)
        {
            DiagnosticAnalyzer = diagnosticAnalyzer;
            DefaultAnalyzerId = id;
            DefaultAnalyzerMessage = message;
            return this;
        }

        public ProjectBuilder WithAnalyzer<T>(string id = null, string message = null) where T : DiagnosticAnalyzer, new()
        {
            return WithAnalyzer(new T(), id, message);
        }

        public ProjectBuilder WithCodeFixProvider(CodeFixProvider codeFixProvider)
        {
            CodeFixProvider = codeFixProvider;
            return this;
        }

        public ProjectBuilder WithCodeFixProvider<T>() where T : CodeFixProvider, new()
        {
            return WithCodeFixProvider(new T());
        }

        public ProjectBuilder ShouldReportDiagnostic(int line, int column, string id = null, string message = null, DiagnosticSeverity? severity = null)
        {
            return ShouldReportDiagnostic(new DiagnosticResult
            {
                Id = id ?? DefaultAnalyzerId,
                Message = message ?? DefaultAnalyzerMessage,
                Severity = severity,
                Locations = new[]
                {
                    new DiagnosticResultLocation(FileName ?? "Test0.cs", line, column),
                },
            });
        }

        public ProjectBuilder ShouldReportDiagnostic(params DiagnosticResult[] expectedDiagnosticResults)
        {
            foreach (var diagnostic in expectedDiagnosticResults)
            {
                ExpectedDiagnosticResults.Add(diagnostic);
            }

            return this;
        }

        public ProjectBuilder ShouldReportDiagnosticWithMessage(string message)
        {
            ExpectedDiagnosticResults[_diagnosticMessageIndex].Message = message;
            _diagnosticMessageIndex++;
            return this;
        }

        public ProjectBuilder ShouldFixCodeWith(string codeFix)
        {
            return ShouldFixCodeWith(index: null, codeFix);
        }

        public ProjectBuilder ShouldFixCodeWith(int? index, string codeFix)
        {
            ExpectedFixedCode = codeFix;
            CodeFixIndex = index;
            return this;
        }
    }
}

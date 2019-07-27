using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TestHelper
{
    public sealed partial class ProjectBuilder
    {
        private int _diagnosticMessageIndex = 0;

        public string FileName { get; private set; }
        public string SourceCode { get; private set; } = "";
        public string EditorConfig { get; private set; }
        public bool IsValidCode { get; private set; } = true;
        public LanguageVersion LanguageVersion { get; private set; } = LanguageVersion.Latest;
        public IList<MetadataReference> References { get; } = new List<MetadataReference>();
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
            var sb = new StringBuilder();
            var lineStart = -1;
            var columnStart = -1;

            var lineIndex = 1;
            var columnIndex = 1;
            for (var i = 0; i < sourceCode.Length; i++)
            {
                var c = sourceCode[i];
                switch (c)
                {
                    case '\n':
                        sb.Append(c);
                        lineIndex++;
                        columnIndex = 1;
                        break;

                    case '[' when lineStart < 0 && Next() == '|':
                        lineStart = lineIndex;
                        columnStart = columnIndex;
                        i++;
                        break;

                    case '|' when lineStart >= 0 && Next() == ']':
                        ShouldReportDiagnostic(new DiagnosticResult
                        {
                            Locations = new[]
                            {
                                new DiagnosticResultLocation("Test0.cs", lineStart, columnStart, lineIndex, columnIndex),
                            },
                        });

                        lineStart = -1;
                        columnStart = -1;
                        i++;
                        break;

                    default:
                        sb.Append(c);
                        columnIndex++;
                        break;
                }

                char Next()
                {
                    if (i + 1 < sourceCode.Length)
                        return sourceCode[i + 1];

                    return default;
                }
            }

            SourceCode = sb.ToString();
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

        private ProjectBuilder ShouldReportDiagnostic(params DiagnosticResult[] expectedDiagnosticResults)
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

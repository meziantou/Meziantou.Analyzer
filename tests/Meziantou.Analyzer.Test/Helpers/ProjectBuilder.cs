using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TestHelper
{
    public partial class ProjectBuilder
    {
        public ProjectBuilder()
        {
            References = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location),
            };
        }

        public string FileName { get; private set; }
        public string SourceCode { get; private set; } = "";
        public string EditorConfig { get; private set; }
        public bool IsValidCode { get; private set; } = true;
        public LanguageVersion LanguageVersion { get; private set; } = LanguageVersion.CSharp7_3;
        public IList<MetadataReference> References { get; }
        public IList<string> ApiReferences { get; } = new List<string>();
        public DiagnosticAnalyzer DiagnosticAnalyzer { get; private set; }
        public CodeFixProvider CodeFixProvider { get; private set; }
        public IList<DiagnosticResult> ExpectedDiagnosticResults { get; private set; }
        public string ExpectedFixedCode { get; private set; }
        public int? CodeFixIndex { get; private set; }
        public string DefaultAnalyzerId { get; set; }
        public string DefaultAnalyzerMessage { get; set; }
        public bool AllowNewCompilerDiagnostics { get; set; }

        public ProjectBuilder AddReference(Type type)
        {
            if (type == typeof(ConcurrentDictionary<,>))
            {
                AddReferenceByName("System.Collections.Concurrent");
                AddReferenceByName("System.Runtime");
            }
            else if (type == typeof(Dictionary<,>))
            {
                AddReferenceByName("System.Collections");
                AddReferenceByName("System.Runtime");
            }
            else if (type == typeof(Enumerable))
            {
                AddReferenceByName("System.Linq");
                AddReferenceByName("System.Runtime");
            }
            else if (type == typeof(IQueryable<>))
            {
                AddReferenceByName("System.Linq");
                AddReferenceByName("System.Linq.Expressions");
                AddReferenceByName("System.Linq.Queryable");
                AddReferenceByName("System.Runtime");
            }
            else if (type == typeof(HashSet<>))
            {
                AddReferenceByName("System.Collections");
            }
            else if (type == typeof(IEnumerable<>))
            {
                AddReferenceByName("System.Runtime");
            }
            else if (type == typeof(Regex))
            {
                AddReferenceByName("System.Runtime");
                AddReferenceByName("System.Text.RegularExpressions");
            }
            else if (type == typeof(System.Threading.Thread))
            {
                AddReferenceByName("System.Threading.Thread");
            }
            else if (type == typeof(System.ComponentModel.InvalidEnumArgumentException))
            {
                AddReferenceByName("System.Runtime");
                AddReferenceByName("System.ComponentModel.Primitives");
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            return this;
        }

        private void AddReferenceByName(string name)
        {
            var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);
            AddReference(trustedAssembliesPaths.Single(p => string.Equals(Path.GetFileNameWithoutExtension(p), name, StringComparison.Ordinal)));
        }

        private ProjectBuilder AddReference(string location)
        {
            References.Add(MetadataReference.CreateFromFile(location));
            return this;
        }

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
            SourceCode = sourceCode;
            return this;
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

        public ProjectBuilder ShouldNotReportDiagnostic()
        {
            return ShouldReportDiagnostic();
        }

        public ProjectBuilder ShouldReportDiagnostic(params DiagnosticResult[] expectedDiagnosticResults)
        {
            if (ExpectedDiagnosticResults == null)
            {
                ExpectedDiagnosticResults = new List<DiagnosticResult>();
            }

            foreach (var diagnostic in expectedDiagnosticResults)
            {
                ExpectedDiagnosticResults.Add(diagnostic);
            }

            return this;
        }

        public ProjectBuilder ShouldFixCodeWith(string codeFix)
        {
            return ShouldFixCodeWith(index: null, codeFix);
        }

        public ProjectBuilder ShouldFixCodeWith(int? index, string codeFix)
        {
            if (ExpectedDiagnosticResults == null)
            {
                ExpectedDiagnosticResults = new List<DiagnosticResult>();
            }

            ExpectedFixedCode = codeFix;
            CodeFixIndex = index;
            return this;
        }

        public ProjectBuilder CodeFixAllowNewCompilerDiagnostics()
        {
            AllowNewCompilerDiagnostics = true;
            return this;
        }
    }
}

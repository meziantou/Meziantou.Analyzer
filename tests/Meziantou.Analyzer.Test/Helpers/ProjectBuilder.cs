using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestHelper
{
    public class ProjectBuilder
    {
        public ProjectBuilder()
        {
            References = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location),
            };
        }

        public string SourceCode { get; private set; } = "";
        public bool IsStatements { get; private set; }

        public bool IsValidCode { get; private set; } = true;

        public LanguageVersion LanguageVersion { get; private set; } = LanguageVersion.Latest;

        public IList<MetadataReference> References { get; }

        public IList<string> ApiReferences { get; } = new List<string>();

        public ProjectBuilder AddReference(Type type)
        {
            return AddReferences(new[] { type.Assembly.Location });
        }

        public ProjectBuilder AddReference(string location)
        {
            return AddReferences(new[] { location });
        }

        public ProjectBuilder AddReferences(IEnumerable<string> locations)
        {
            foreach (var location in locations)
            {
                References.Add(MetadataReference.CreateFromFile(location));
            }

            return this;
        }

        public ProjectBuilder AddApiReference(string name)
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

        public ProjectBuilder AddConcurrentDictionaryApi() => AddApiReference("System.Collections.Concurrent.ConcurrentDictionary");
        public ProjectBuilder AddRegexApi() => AddApiReference("System.Text.RegularExpressions.Regex");
        public ProjectBuilder AddWpfApi() => AddApiReference("System.Windows.Window");

        public ProjectBuilder WithSource(string content)
        {
            SourceCode = content;
            IsStatements = false;
            return this;
        }

        public ProjectBuilder WithStatement(string content)
        {
            SourceCode = "class Test{void Method(){" + content + "}}";
            IsStatements = true;
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
    }
}


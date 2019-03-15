using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TestHelper
{
    public class ProjectBuilder
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

        public string SourceCode { get; private set; } = "";
        public bool IsStatements { get; private set; }

        public bool IsValidCode { get; private set; } = true;

        public LanguageVersion LanguageVersion { get; private set; } = LanguageVersion.Latest;

        public IList<MetadataReference> References { get; }

        public IList<string> ApiReferences { get; } = new List<string>();

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

        public ProjectBuilder AddXUnit() => AddApiReference("XUnit");

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


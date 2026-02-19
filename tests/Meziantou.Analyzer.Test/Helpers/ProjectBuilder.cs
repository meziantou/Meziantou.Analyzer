using System.Collections.Concurrent;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Meziantou.Analyzer.Annotations;
using Meziantou.Analyzer.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHelper;

public sealed partial class ProjectBuilder
{
    private static readonly ConcurrentDictionary<string, Lazy<Task<string[]>>> NuGetPackagesCache = new(StringComparer.Ordinal);

    private int _diagnosticMessageIndex;

    public OutputKind OutputKind { get; private set; } = OutputKind.DynamicallyLinkedLibrary;
    public string? FileName { get; private set; }
    public string SourceCode { get; private set; } = "";
    public Dictionary<string, string>? AnalyzerConfiguration { get; private set; }
    public Dictionary<string, string>? AdditionalFiles { get; private set; }
    public bool IsValidCode { get; private set; } = true;
    public bool IsValidFixCode { get; private set; } = true;
    public LanguageVersion LanguageVersion { get; private set; } = LanguageVersion.Latest;
    public TargetFramework TargetFramework { get; private set; } = TargetFramework.NetLatest;
    public IList<MetadataReference> References { get; } = [];
    public IList<string> ApiReferences { get; } = [];
    public IList<DiagnosticAnalyzer> DiagnosticAnalyzer { get; } = [];
    public IList<AnalyzerReference> AnalyzerReferences { get; } = [];
    public CodeFixProvider? CodeFixProvider { get; private set; }
    public IList<DiagnosticResult> ExpectedDiagnosticResults { get; } = [];
    public string? ExpectedFixedCode { get; private set; }
    public int? CodeFixIndex { get; private set; }
    public bool UseBatchFixer { get; private set; }
    public string? DefaultAnalyzerId { get; set; }
    public string? DefaultAnalyzerMessage { get; set; }

    private static async Task<string[]> GetNuGetReferences(string packageName, string version, string[] includedPaths)
    {
        var bytes = Encoding.UTF8.GetBytes(packageName + '@' + version + ':' + string.Join(',', includedPaths));
        var hash = SHA256.HashData(bytes);
        var key = Convert.ToBase64String(hash).Replace('/', '_');
        var task = NuGetPackagesCache.GetOrAdd(key, _ => new Lazy<Task<string[]>>(Download));
        return await task.Value.ConfigureAwait(false);

        async Task<string[]> Download()
        {
            var cacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Meziantou.AnalyzerTests", "ref", key);
            bool IsCacheValid() => Directory.Exists(cacheFolder) && Directory.EnumerateFileSystemEntries(cacheFolder).Any();

            if (!IsCacheValid())
            {
                var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

                Directory.CreateDirectory(tempFolder);
                await using var stream = await SharedHttpClient.Instance.GetStreamAsync(new Uri($"https://www.nuget.org/api/v2/package/{packageName}/{version}")).ConfigureAwait(false);
                await using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

                foreach (var entry in zip.Entries.Where(file => includedPaths.Any(path => file.FullName.StartsWith(path, StringComparison.Ordinal))))
                {
                    await entry.ExtractToFileAsync(Path.Combine(tempFolder, entry.Name), overwrite: true);
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(cacheFolder)!);
                    Directory.Move(tempFolder, cacheFolder);
                }
                catch (Exception ex)
                {
                    if (!IsCacheValid())
                    {
                        throw new InvalidOperationException("Cannot download NuGet package " + packageName + "@" + version + "\n" + ex);
                    }
                }
            }

            var dlls = Directory.GetFiles(cacheFolder, "*.dll");

            // Filter invalid .NET assembly
            var result = new List<string>();
            foreach (var dll in dlls)
            {
                if (Path.GetFileName(dll) == "System.EnterpriseServices.Wrapper.dll")
                    continue;

                try
                {
                    using var stream = File.OpenRead(dll);
                    using var peFile = new PEReader(stream);
                    var metadataReader = peFile.GetMetadataReader();
                    result.Add(dll);
                }
                catch
                {
                }
            }

            return [.. result];
        }
    }

    public ProjectBuilder AddNuGetReference(string packageName, string version, string pathPrefix)
    {
        foreach (var reference in GetNuGetReferences(packageName, version, [pathPrefix]).Result)
        {
            References.Add(MetadataReference.CreateFromFile(reference));
        }

        return this;
    }

    public ProjectBuilder WithAnalyzerFromNuGet(string packageName, string version, string[] paths, string[] ruleIds)
    {
        var ruleFound = false;
        var references = GetNuGetReferences(packageName, version, paths).Result;
        foreach (var reference in references)
        {
            var assembly = Assembly.LoadFrom(reference);
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
                    continue;

                var instance = (DiagnosticAnalyzer)Activator.CreateInstance(type)!;
                if (instance.SupportedDiagnostics.Any(d => ruleIds.Contains(d.Id, StringComparer.Ordinal)))
                {
                    DiagnosticAnalyzer.Add(instance);
                    ruleFound = true;
                }
            }
        }

        if (!ruleFound)
            throw new InvalidOperationException("Rule id not found");

        return this;
    }

#if ROSLYN5_0
    public ProjectBuilder WithMicrosoftCodeAnalysisNetAnalyzers(params string[] ruleIds) =>
        WithAnalyzerFromNuGet(
            "Microsoft.CodeAnalysis.NetAnalyzers",
            "10.0.100",
            paths: ["analyzers/dotnet/cs/", "analyzers/dotnet/Microsoft."],
            ruleIds);
#else
    public ProjectBuilder WithMicrosoftCodeAnalysisNetAnalyzers(params string[] ruleIds) =>
        WithAnalyzerFromNuGet(
            "Microsoft.CodeAnalysis.NetAnalyzers",
            "9.0.0",
            paths: ["analyzers/dotnet/cs/Microsoft.CodeAnalysis"],
            ruleIds);
#endif

    public ProjectBuilder WithMicrosoftCodeAnalysisCSharpCodeStyleAnalyzers(params string[] ruleIds)
    {
        AddNuGetReference("Microsoft.Bcl.AsyncInterfaces", "9.0.7", "lib/netstandard2.1/");
#if ROSLYN5_0
        return WithAnalyzerFromNuGet(
            "Microsoft.CodeAnalysis.CSharp.CodeStyle",
            "5.0.0-2.final",
            paths: ["analyzers/dotnet/cs/", "analyzers/dotnet/Microsoft.CodeAnalysis"],
            ruleIds);
#else
        return WithAnalyzerFromNuGet(
            "Microsoft.CodeAnalysis.CSharp.CodeStyle",
            "4.14.0",
            paths: ["analyzers/dotnet/cs/"],
            ruleIds);
#endif
    }

    public ProjectBuilder AddMSTestApi() => AddNuGetReference("MSTest.TestFramework", "2.1.1", "lib/netstandard1.0/");

    public ProjectBuilder AddNUnitApi() => AddNuGetReference("NUnit", "3.12.0", "lib/netstandard2.0/");

    public ProjectBuilder AddXUnitApi() =>
        AddNuGetReference("xunit.extensibility.core", "2.4.1", "lib/netstandard1.1/")
        .AddNuGetReference("xunit.assert", "2.4.1", "lib/netstandard1.1/");

    public ProjectBuilder AddAsyncInterfaceApi() =>
        AddNuGetReference("Microsoft.Bcl.AsyncInterfaces", "1.1.1", "ref/netstandard2.0/")
        .AddNuGetReference("System.Threading.Tasks.Extensions", "4.5.4", "lib/netstandard2.0/")
        .AddNuGetReference("System.Runtime.CompilerServices.Unsafe", "4.7.1", "ref/netstandard2.0/");

    public ProjectBuilder AddSystemTextJson() => AddNuGetReference("System.Text.Json", "4.7.2", "lib/netstandard2.0/");

    public ProjectBuilder AddMeziantouAttributes()
    {
        var location = typeof(RequireNamedArgumentAttribute).Assembly.Location;
        References.Add(MetadataReference.CreateFromFile(location));
        return this;
    }

    public ProjectBuilder WithOutputKind(OutputKind outputKind)
    {
        OutputKind = outputKind;
        return this;
    }

    public ProjectBuilder WithSourceCode([StringSyntax("C#-test")] string sourceCode) =>
        WithSourceCode(fileName: null, sourceCode);

    public ProjectBuilder WithSourceCode(string? fileName, [StringSyntax("C#-test")] string sourceCode)
    {
        FileName = fileName;
        ParseSourceCode(sourceCode);
        return this;
    }

    /// <summary>
    /// <list type="bullet">
    ///   <item>[|code|]</item>
    ///   <item>{|ruleId:code|}</item>
    /// </list>
    /// </summary>
    /// <param name="sourceCode"></param>
    private void ParseSourceCode(string sourceCode)
    {
        var sb = new StringBuilder();
        var lineStart = -1;
        var columnStart = -1;

        var lineIndex = 1;
        var columnIndex = 1;
        char endChar = default;
        string? ruleId = default;
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

                case '{' when lineStart < 0 && Next() == '|':
                    lineStart = lineIndex;
                    columnStart = columnIndex;
                    endChar = '}';
                    i += 2;
                    ruleId = TakeUntil(':');
                    i += ruleId.Length;
                    break;

                case '[' when lineStart < 0 && Next() == '|':
                    lineStart = lineIndex;
                    columnStart = columnIndex;
                    i++;
                    endChar = ']';
                    break;

                case '|' when lineStart >= 0 && Next() == endChar:
                    ShouldReportDiagnostic(new DiagnosticResult
                    {
                        Id = ruleId ?? DefaultAnalyzerId,
                        Message = DefaultAnalyzerMessage,
                        Locations = [new DiagnosticResultLocation(FileName ?? "Test0.cs", lineStart, columnStart, lineIndex, columnIndex)],
                    });

                    lineStart = -1;
                    columnStart = -1;
                    endChar = default;
                    ruleId = default;
                    i++;
                    break;

                default:
                    sb.Append(c);
                    columnIndex++;
                    break;
            }

            char Next() => i + 1 < sourceCode.Length ? sourceCode[i + 1] : default;
            string TakeUntil(char c)
            {
                var span = sourceCode.AsSpan(i);
                var index = span.IndexOf(c);
                if (index < 0)
                    return span.ToString();

                return span[0..index].ToString();
            }
        }

        SourceCode = sb.ToString();
    }

    public ProjectBuilder WithAnalyzerConfiguration(Dictionary<string, string> configuration)
    {
        AnalyzerConfiguration = configuration;
        return this;
    }

    public ProjectBuilder AddAnalyzerConfiguration(string key, string value)
    {
        AnalyzerConfiguration ??= [];
        AnalyzerConfiguration[key] = value;
        return this;
    }

    public ProjectBuilder AddAdditionalFile(string path, string content)
    {
        AdditionalFiles ??= [];
        AdditionalFiles[path] = content;
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
        IsValidFixCode = false;
        return this;
    }

    public ProjectBuilder WithNoFixCompilation()
    {
        IsValidFixCode = false;
        return this;
    }

    public ProjectBuilder WithSourceGeneratorFromAssembly(string assemblyPath)
    {
        AnalyzerReferences.Add(new AnalyzerFileReference(assemblyPath, AnalyzerAssemblyLoader.Instance));
        return this;
    }

    public ProjectBuilder WithSourceGeneratorsFromNuGet(string packageName, string version, string pathPrefix)
    {
        var references = GetNuGetReferences(packageName, version, [pathPrefix]).Result;
        foreach (var reference in references)
        {
            AnalyzerReferences.Add(new AnalyzerFileReference(reference, AnalyzerAssemblyLoader.Instance));
        }

        return this;
    }

    public ProjectBuilder WithDefaultAnalyzerId(string id)
    {
        DefaultAnalyzerId = id;
        return this;
    }

    public ProjectBuilder WithAnalyzer(DiagnosticAnalyzer diagnosticAnalyzer, string? id = null, string? message = null)
    {
        DiagnosticAnalyzer.Add(diagnosticAnalyzer);
        DefaultAnalyzerId = id;
        DefaultAnalyzerMessage = message;
        return this;
    }

    public ProjectBuilder WithAnalyzer<T>(string? id = null, string? message = null) where T : DiagnosticAnalyzer, new() =>
        WithAnalyzer(new T(), id, message);

    public ProjectBuilder WithCodeFixProvider(CodeFixProvider codeFixProvider)
    {
        CodeFixProvider = codeFixProvider;
        return this;
    }

    public ProjectBuilder WithCodeFixProvider<T>() where T : CodeFixProvider, new() =>
        WithCodeFixProvider(new T());

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
        if (_diagnosticMessageIndex >= ExpectedDiagnosticResults.Count)
            throw new InvalidOperationException("Did you forget to annotate the code with [||]?");

        ExpectedDiagnosticResults[_diagnosticMessageIndex].Message = message;
        _diagnosticMessageIndex++;
        return this;
    }

    public ProjectBuilder ShouldFixCodeWith([StringSyntax("C#-test")] string codeFix) =>
        ShouldFixCodeWith(index: null, codeFix);

    public ProjectBuilder ShouldFixCodeWith(int? index, [StringSyntax("C#-test")] string codeFix)
    {
        ExpectedFixedCode = codeFix;
        CodeFixIndex = index;
        return this;
    }

    public ProjectBuilder ShouldBatchFixCodeWith([StringSyntax("C#-test")] string codeFix) =>
        ShouldBatchFixCodeWith(index: null, codeFix);

    public ProjectBuilder ShouldBatchFixCodeWith(int? index, [StringSyntax("C#-test")] string codeFix)
    {
        ExpectedFixedCode = codeFix;
        CodeFixIndex = index;
        UseBatchFixer = true;
        return this;
    }

    public ProjectBuilder WithTargetFramework(TargetFramework targetFramework)
    {
        TargetFramework = targetFramework;
        return this;
    }
}

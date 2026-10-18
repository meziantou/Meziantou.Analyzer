using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Fuzz;

/// <summary>
/// The analyzers and code fixers under test, and the options used to compile the fuzzed code.
/// They are created once per process, as creating them is much slower than analyzing an input.
/// </summary>
internal static class Components
{
    private static readonly Assembly[] Assemblies = [typeof(Rules.CommaAnalyzer).Assembly, typeof(Rules.CommaFixer).Assembly];

    // The assemblies of the application that must not be referenced by the fuzzed code
    private static readonly HashSet<string> ExcludedReferences = new(StringComparer.OrdinalIgnoreCase)
    {
        "dnlib",
        "Meziantou.Analyzer",
        "Meziantou.Analyzer.CodeFixers",
        "Meziantou.Analyzer.Fuzz",
        "Meziantou.Framework.FullPath",
        "SharpFuzz",
        "SharpFuzz.Common",
    };

    /// <summary>The analyzers and the diagnostic suppressors.</summary>
    public static ImmutableArray<DiagnosticAnalyzer> Analyzers { get; } = CreateInstances<DiagnosticAnalyzer>();

    public static ImmutableArray<CodeFixProvider> CodeFixProviders { get; } = CreateInstances<CodeFixProvider>();

    /// <summary>
    /// The assemblies of the application: the .NET and ASP.NET Core frameworks.
    /// </summary>
    public static ImmutableArray<MetadataReference> MetadataReferences { get; } = CreateMetadataReferences();

    /// <summary>
    /// The source generators of the .NET reference pack, such as the one that implements the members annotated with <c>[GeneratedRegex]</c>,
    /// so the code fixes that rely on them produce code that compiles.
    /// </summary>
    public static ImmutableArray<AnalyzerReference> SourceGenerators { get; } = CreateSourceGenerators();

    public static CSharpParseOptions ParseOptions { get; } = new(LanguageVersion.Preview);

    /// <summary>
    /// The rules disabled by default are enabled, so all the rules run. <see cref="MetadataImportOptions.All"/> is the option of the tests,
    /// which some rules need to analyze the non-public members of the referenced assemblies.
    /// </summary>
    public static CSharpCompilationOptions CompilationOptions { get; } = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable,
            allowUnsafe: true)
        .WithMetadataImportOptions(MetadataImportOptions.All)
        .WithSpecificDiagnosticOptions(Analyzers
            .SelectMany(analyzer => analyzer.SupportedDiagnostics)
            .Select(descriptor => descriptor.Id)
            .Distinct(StringComparer.Ordinal)
            .Select(id => KeyValuePair.Create(id, ReportDiagnostic.Warn)));

    public static IEnumerable<object> All => Analyzers.Cast<object>().Concat(CodeFixProviders);

    public static string GetName(object component) => component.GetType().FullName!;

    public static ImmutableArray<string> GetDiagnosticIds(object component)
    {
        IEnumerable<string> ids = component switch
        {
            DiagnosticSuppressor suppressor => suppressor.SupportedSuppressions.Select(suppression => suppression.SuppressedDiagnosticId),
            DiagnosticAnalyzer analyzer => analyzer.SupportedDiagnostics.Select(descriptor => descriptor.Id),
            CodeFixProvider codeFixProvider => codeFixProvider.FixableDiagnosticIds,
            _ => [],
        };

        return [.. ids.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }

    public static object? FindByName(string name) => All.FirstOrDefault(component => GetName(component) == name);

    /// <summary>
    /// Find the analyzer or the code fixer that declares the first frame of a stack trace that belongs to one.
    /// The stack traces separate the nested types with a '.', whereas <see cref="Type.FullName"/> uses a '+'.
    /// </summary>
    public static object? FindByStackFrames(IEnumerable<string> frames)
    {
        foreach (var frame in frames)
        {
            var component = All
                .Where(component => frame.StartsWith(GetName(component).Replace('+', '.') + ".", StringComparison.Ordinal))
                .MaxBy(component => GetName(component).Length);
            if (component is not null)
                return component;
        }

        return null;
    }

    private static ImmutableArray<T> CreateInstances<T>()
    {
        return
        [
            .. Assemblies
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Where(type => !type.IsAbstract && typeof(T).IsAssignableFrom(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .Select(type => (T)Activator.CreateInstance(type)!),
        ];
    }

    private static ImmutableArray<AnalyzerReference> CreateSourceGenerators()
    {
        // The runtime is in <dotnet>/shared/Microsoft.NETCore.App/<version>, and the reference pack of the SDK in <dotnet>/packs/Microsoft.NETCore.App.Ref/<version>
        var runtimeDirectory = new DirectoryInfo(Path.GetDirectoryName(typeof(object).Assembly.Location)!);
        var packDirectory = new DirectoryInfo(Path.Combine(runtimeDirectory.Parent!.Parent!.Parent!.FullName, "packs", "Microsoft.NETCore.App.Ref"));
        if (!packDirectory.Exists)
            return [];

        var versionDirectory = packDirectory.EnumerateDirectories()
            .Where(directory => directory.Name == runtimeDirectory.Name || directory.Name.StartsWith(Environment.Version.Major.ToString(CultureInfo.InvariantCulture) + ".", StringComparison.Ordinal))
            .OrderByDescending(directory => directory.Name == runtimeDirectory.Name)
            .ThenByDescending(directory => Version.TryParse(directory.Name, out var version) ? version : null)
            .FirstOrDefault();

        var generatorDirectory = versionDirectory is null ? null : new DirectoryInfo(Path.Combine(versionDirectory.FullName, "analyzers", "dotnet", "cs"));
        if (generatorDirectory is not { Exists: true })
            return [];

        var paths = generatorDirectory.EnumerateFiles("*.dll").Select(file => file.FullName).Order(StringComparer.Ordinal).ToList();
        var loader = new GeneratorAssemblyLoader(paths);
        return [.. paths.Select(path => new AnalyzerFileReference(path, loader))];
    }

    private static ImmutableArray<MetadataReference> CreateMetadataReferences()
    {
        var paths = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries) ?? [];
        return
        [
            .. paths
                .Where(path => Path.GetFileNameWithoutExtension(path) is var name
                    && !ExcludedReferences.Contains(name)
                    && !name.StartsWith("Microsoft.CodeAnalysis", StringComparison.OrdinalIgnoreCase)
                    && !name.StartsWith("System.Composition", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal)
                .Select(path => MetadataReference.CreateFromFile(path)),
        ];
    }
}

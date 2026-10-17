using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer.Fuzz;

/// <summary>
/// Load the source generators of the .NET reference pack in their own context, as the tests do (see GeneratorAssemblyLoader in the test project).
/// The assemblies the generators share with the harness, such as <c>Microsoft.CodeAnalysis</c>, resolve to the ones already loaded.
/// </summary>
internal sealed class GeneratorAssemblyLoader : IAnalyzerAssemblyLoader
{
    private readonly Dictionary<string, string> _pathsBySimpleName = new(StringComparer.OrdinalIgnoreCase);
    private readonly AssemblyLoadContext _context;

    public GeneratorAssemblyLoader(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            _pathsBySimpleName[Path.GetFileNameWithoutExtension(path)] = path;
        }

        _context = new AssemblyLoadContext("SourceGenerators", isCollectible: false);
        _context.Resolving += Resolve;
    }

    public void AddDependencyLocation(string fullPath)
    {
    }

    public Assembly LoadFromPath(string fullPath) => _context.LoadFromAssemblyPath(fullPath);

    private Assembly? Resolve(AssemblyLoadContext context, AssemblyName assemblyName) =>
        assemblyName.Name is { } simpleName && _pathsBySimpleName.TryGetValue(simpleName, out var path)
            ? context.LoadFromAssemblyPath(path)
            : null;
}

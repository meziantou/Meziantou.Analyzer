using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;

namespace Meziantou.Analyzer.Test.Harness;

/// <summary>
/// Loads the source generators of one .NET reference pack in their own context. The reference packs of the
/// different .NET versions ship assemblies with the same name and different versions, which the default context
/// cannot hold at the same time. The assemblies the generators share with the test, such as
/// <c>Microsoft.CodeAnalysis</c>, are not registered here, so they resolve to the ones already loaded.
/// </summary>
internal sealed class GeneratorAssemblyLoader(string name)
{
    private readonly ConcurrentDictionary<string, string> _pathsBySimpleName = new(StringComparer.OrdinalIgnoreCase);
    private readonly AssemblyLoadContext _context = CreateContext(name);

    private static AssemblyLoadContext CreateContext(string name) => new("SourceGenerators " + name, isCollectible: false);

    public Assembly Load(string path)
    {
        _pathsBySimpleName[Path.GetFileNameWithoutExtension(path)] = path;
        _context.Resolving -= Resolve;
        _context.Resolving += Resolve;
        return _context.LoadFromAssemblyPath(path);
    }

    private Assembly? Resolve(AssemblyLoadContext context, AssemblyName assemblyName) =>
        assemblyName.Name is { } simpleName && _pathsBySimpleName.TryGetValue(simpleName, out var path)
            ? context.LoadFromAssemblyPath(path)
            : null;
}

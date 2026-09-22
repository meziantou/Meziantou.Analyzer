using System.Reflection;
using System.Runtime.Loader;

namespace Meziantou.Analyzer.Test.Harness;

/// <summary>
/// Loads the source generators of one .NET reference pack in their own context. The reference packs of the
/// different .NET versions ship assemblies with the same name and different versions, which the default context
/// cannot hold at the same time. The assemblies the generators share with the test, such as
/// <c>Microsoft.CodeAnalysis</c>, are not registered here, so they resolve to the ones already loaded.
/// </summary>
/// <remarks>
/// The paths of the pack are all registered before any assembly is loaded, as a generator can depend on another
/// assembly of the pack: <c>Microsoft.Interop.ComInterfaceGenerator</c> depends on
/// <c>Microsoft.Interop.SourceGeneration</c>, which the order the assemblies are enumerated in does not guarantee
/// to be loaded first.
/// </remarks>
internal sealed class GeneratorAssemblyLoader
{
    // Only read once the constructor has registered the paths, so it is safe to resolve from several threads
    private readonly Dictionary<string, string> _pathsBySimpleName = new(StringComparer.OrdinalIgnoreCase);
    private readonly AssemblyLoadContext _context;

    public GeneratorAssemblyLoader(string name, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            _pathsBySimpleName[Path.GetFileNameWithoutExtension(path)] = path;
        }

        _context = new AssemblyLoadContext("SourceGenerators " + name, isCollectible: false);
        _context.Resolving += Resolve;
    }

    public Assembly Load(string path) => _context.LoadFromAssemblyPath(path);

    private Assembly? Resolve(AssemblyLoadContext context, AssemblyName assemblyName) =>
        assemblyName.Name is { } simpleName && _pathsBySimpleName.TryGetValue(simpleName, out var path)
            ? context.LoadFromAssemblyPath(path)
            : null;
}

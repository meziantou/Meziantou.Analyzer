using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace TestHelper;

internal sealed class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
{
    private readonly ConcurrentDictionary<string, string> _assemblyPathsBySimpleName = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Assembly> _loadedAssembliesByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly AssemblyLoadContext _loadContext = new("AnalyzerAssemblyLoader", isCollectible: true);

    public AnalyzerAssemblyLoader()
    {
        _loadContext.Resolving += ResolveAssembly;
    }

    public void AddDependencyLocation(string fullPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(fullPath);
        if (string.IsNullOrEmpty(fileName))
            return;

        _assemblyPathsBySimpleName[fileName] = fullPath;
    }

    public Assembly LoadFromPath(string fullPath)
    {
        AddDependencyLocation(fullPath);
        return _loadedAssembliesByPath.GetOrAdd(fullPath, static (path, loadContext) => loadContext.LoadFromAssemblyPath(path), _loadContext);
    }

    private Assembly? ResolveAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        if (assemblyName.Name is null)
            return null;

        if (!_assemblyPathsBySimpleName.TryGetValue(assemblyName.Name, out var path))
            return null;

        return _loadedAssembliesByPath.GetOrAdd(path, static (fullPath, loadContext) => loadContext.LoadFromAssemblyPath(fullPath), _loadContext);
    }
}

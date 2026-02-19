using System.Reflection;
using Microsoft.CodeAnalysis;

namespace TestHelper;

internal sealed class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
{
    public static readonly AnalyzerAssemblyLoader Instance = new();

    private AnalyzerAssemblyLoader()
    {
    }

    public void AddDependencyLocation(string fullPath)
    {
    }

    public Assembly LoadFromPath(string fullPath)
    {
        return Assembly.LoadFrom(fullPath);
    }
}

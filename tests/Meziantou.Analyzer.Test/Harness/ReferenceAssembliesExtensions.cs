using Microsoft.CodeAnalysis.Testing;

namespace Meziantou.Analyzer.Test.Harness;

internal static class ReferenceAssembliesExtensions
{
    /// <summary>
    /// Adds the reference assemblies of ASP.NET Core, in the version matching the .NET version the tests use by default.
    /// </summary>
    public static ReferenceAssemblies AddAspNetCore(this ReferenceAssemblies referenceAssemblies) =>
        referenceAssemblies.AddAspNetCore(AnalyzerTestDefaults.DotNetVersion);

    /// <inheritdoc cref="AddAspNetCore(ReferenceAssemblies)" />
    public static ReferenceAssemblies AddAspNetCore(this ReferenceAssemblies referenceAssemblies, string version) =>
        referenceAssemblies.AddPackages([new PackageIdentity("Microsoft.AspNetCore.App.Ref", version)]);
}

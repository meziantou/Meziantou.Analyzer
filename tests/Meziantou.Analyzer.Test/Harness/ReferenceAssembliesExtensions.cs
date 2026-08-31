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

    /// <summary>
    /// Adds the MSTest API.
    /// </summary>
    public static ReferenceAssemblies AddMSTestApi(this ReferenceAssemblies referenceAssemblies) =>
        referenceAssemblies.AddPackages([new PackageIdentity("MSTest.TestFramework", "2.1.1")]);

    /// <summary>
    /// Adds the xUnit v3 API.
    /// </summary>
    public static ReferenceAssemblies AddXUnitApi(this ReferenceAssemblies referenceAssemblies) =>
        referenceAssemblies.AddPackages([
            new PackageIdentity("xunit.v3.extensibility.core", "4.0.0"),
            new PackageIdentity("xunit.v3.assert", "4.0.0"),
        ]);

    /// <summary>
    /// Adds the NUnit API.
    /// </summary>
    public static ReferenceAssemblies AddNUnitApi(this ReferenceAssemblies referenceAssemblies) =>
        referenceAssemblies.AddPackages([new PackageIdentity("NUnit", "3.12.0")]);
}

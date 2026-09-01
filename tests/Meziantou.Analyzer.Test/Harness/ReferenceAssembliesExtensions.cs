using Microsoft.CodeAnalysis.Testing;

namespace Meziantou.Analyzer.Test.Harness;

/// <summary>
/// The packages a test can reference, so that the tests referencing the same package use the same version.
/// Every method uses the latest version of the package unless the test asks for a specific one.
/// </summary>
internal static class ReferenceAssembliesExtensions
{
    /// <summary>
    /// Adds the reference assemblies of ASP.NET Core, in the version matching the .NET version the tests use by default.
    /// </summary>
    public static ReferenceAssemblies AddAspNetCore(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([new PackageIdentity("Microsoft.AspNetCore.App.Ref", version ?? AnalyzerTestDefaults.DotNetVersion)]);

    /// <summary>
    /// Adds the attributes of BenchmarkDotNet, such as <c>[Benchmark]</c>.
    /// </summary>
    public static ReferenceAssemblies AddBenchmarkDotNet(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([new PackageIdentity("BenchmarkDotNet.Annotations", version ?? "0.15.8")]);

    /// <summary>
    /// Adds the data classification API, such as <c>[PersonalData]</c>.
    /// </summary>
    public static ReferenceAssemblies AddComplianceAbstractions(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([new PackageIdentity("Microsoft.Extensions.Compliance.Abstractions", version ?? "10.9.0")]);

    /// <summary>
    /// Adds Entity Framework Core.
    /// </summary>
    public static ReferenceAssemblies AddEntityFrameworkCore(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([
            new PackageIdentity("Microsoft.EntityFrameworkCore", version ?? "10.0.11"),
            new PackageIdentity("Microsoft.EntityFrameworkCore.Abstractions", version ?? "10.0.11"),
        ]);

    /// <summary>
    /// Adds the JavaScript interop API of Blazor WebAssembly, in the version matching the .NET version the tests use by default.
    /// </summary>
    public static ReferenceAssemblies AddJSInterop(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([new PackageIdentity("Microsoft.JSInterop.WebAssembly", version ?? AnalyzerTestDefaults.DotNetVersion)]);

    /// <summary>
    /// Adds <c>ILogger</c> and its extension methods.
    /// </summary>
    public static ReferenceAssemblies AddLoggingAbstractions(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([new PackageIdentity("Microsoft.Extensions.Logging.Abstractions", version ?? "10.0.11")]);

    /// <summary>
    /// Adds <c>Meziantou.Framework</c>.
    /// </summary>
    public static ReferenceAssemblies AddMeziantouFramework(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([new PackageIdentity("Meziantou.Framework", version ?? "6.0.2")]);

    /// <summary>
    /// Adds the assertions of <c>Meziantou.Framework</c>.
    /// </summary>
    public static ReferenceAssemblies AddMeziantouFrameworkAssertions(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([new PackageIdentity("Meziantou.Framework.Assertions", version ?? "2.0.5")]);

    /// <summary>
    /// Adds Moq.
    /// </summary>
    public static ReferenceAssemblies AddMoq(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([new PackageIdentity("Moq", version ?? "4.20.72")]);

    /// <summary>
    /// Adds the MSTest API.
    /// </summary>
    public static ReferenceAssemblies AddMSTest(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([new PackageIdentity("MSTest.TestFramework", version ?? "4.3.3")]);

    /// <summary>
    /// Adds <c>Newtonsoft.Json</c>.
    /// </summary>
    public static ReferenceAssemblies AddNewtonsoftJson(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([new PackageIdentity("Newtonsoft.Json", version ?? "13.0.4")]);

    /// <summary>
    /// Adds the NUnit API.
    /// </summary>
    public static ReferenceAssemblies AddNUnit(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([new PackageIdentity("NUnit", version ?? "4.6.1")]);

    /// <summary>
    /// Adds Serilog.
    /// </summary>
    public static ReferenceAssemblies AddSerilog(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([new PackageIdentity("Serilog", version ?? "4.4.0")]);

    /// <summary>
    /// Adds the SQLite ADO.NET provider.
    /// </summary>
    public static ReferenceAssemblies AddSqlite(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([new PackageIdentity("Microsoft.Data.Sqlite.Core", version ?? "10.0.11")]);

    /// <summary>
    /// Adds <c>YamlDotNet</c>.
    /// </summary>
    public static ReferenceAssemblies AddYamlDotNet(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([new PackageIdentity("YamlDotNet", version ?? "18.1.0")]);

    /// <summary>
    /// Adds the xUnit v3 API.
    /// </summary>
    public static ReferenceAssemblies AddXunitV3(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([
            new PackageIdentity("xunit.v3.extensibility.core", version ?? "4.0.0"),
            new PackageIdentity("xunit.v3.assert", version ?? "4.0.0"),
        ]);

    /// <summary>
    /// Adds the xUnit v2 API, which the tests asserting the behavior of the previous major version rely on.
    /// </summary>
    public static ReferenceAssemblies AddXunitV2(this ReferenceAssemblies referenceAssemblies, string? version = null) =>
        referenceAssemblies.AddPackages([
            new PackageIdentity("xunit.extensibility.core", version ?? "2.9.3"),
            new PackageIdentity("xunit.assert", version ?? "2.9.3"),
        ]);
}

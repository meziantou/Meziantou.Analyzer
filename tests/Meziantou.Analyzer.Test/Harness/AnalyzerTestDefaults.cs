using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace Meziantou.Analyzer.Test.Harness;

/// <summary>
/// The defaults shared by the tests based on <see href="https://github.com/dotnet/roslyn-sdk">Microsoft.CodeAnalysis.Testing</see>,
/// so that they compile the same code as the tests based on <see cref="TestHelper.ProjectBuilder"/>.
/// </summary>
internal static class AnalyzerTestDefaults
{
    /// <summary>
    /// <see cref="TestHelper.ProjectBuilder"/> parses the code with <see cref="LanguageVersion.Latest"/>,
    /// whereas the default of the testing library is <see cref="LanguageVersion.Default"/>.
    /// </summary>
    public const LanguageVersion LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest;

    /// <summary>
    /// The version of the .NET packages the tests reference by default, shared by the runtime reference assemblies
    /// and the packages shipped with them, such as the one of <see cref="ReferenceAssembliesExtensions.AddAspNetCore(ReferenceAssemblies)"/>.
    /// </summary>
    public const string DotNetVersion = "11.0.0-preview.7.26381.103";

    /// <summary>
    /// The equivalent of <see cref="TargetFramework.NetLatest"/>, which is the target framework
    /// <see cref="TestHelper.ProjectBuilder"/> uses when a test does not configure one.
    /// </summary>
    public static readonly ReferenceAssemblies ReferenceAssemblies = new(
        "net11.0",
        new PackageIdentity("Microsoft.NETCore.App.Ref", DotNetVersion),
        Path.Combine("ref", "net11.0"));

    /// <summary>
    /// <see cref="TestHelper.ProjectBuilder"/> compiles with <see cref="MetadataImportOptions.All"/>, which the rules
    /// that analyze the non-public members of the referenced assemblies rely on.
    /// </summary>
    public static Solution ConfigureCompilationOptions(Solution solution, ProjectId projectId)
    {
        var compilationOptions = (CSharpCompilationOptions)solution.GetProject(projectId)!.CompilationOptions!;
        return solution.WithProjectCompilationOptions(projectId, compilationOptions.WithMetadataImportOptions(MetadataImportOptions.All));
    }
}

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace Meziantou.Analyzer.Test.Harness;

/// <summary>
/// The defaults shared by the tests based on <see href="https://github.com/dotnet/roslyn-sdk">Microsoft.CodeAnalysis.Testing</see>,
/// so that every test compiles the code the same way.
/// </summary>
internal static class AnalyzerTestDefaults
{
    /// <summary>
    /// The tests parse the code with <see cref="LanguageVersion.Latest"/>,
    /// whereas the default of the testing library is <see cref="LanguageVersion.Default"/>.
    /// </summary>
    public const LanguageVersion LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest;

    /// <summary>
    /// The version of the .NET packages the tests reference by default, shared by the runtime reference assemblies
    /// and the packages shipped with them, such as the one of <see cref="ReferenceAssembliesExtensions.AddAspNetCore(ReferenceAssemblies)"/>.
    /// </summary>
    public const string DotNetVersion = "11.0.0-preview.7.26381.103";

    /// <summary>
    /// The target framework a test compiles against when it does not configure one.
    /// </summary>
    public static readonly ReferenceAssemblies ReferenceAssemblies = new(
        "net11.0",
        new PackageIdentity("Microsoft.NETCore.App.Ref", DotNetVersion),
        Path.Combine("ref", "net11.0"));

    /// <summary>
    /// The source generators shipped with the .NET reference pack of a target framework, such as the one that
    /// implements the partial members annotated with <c>[GeneratedRegex]</c>.
    /// </summary>
    public static IEnumerable<Type> GetFrameworkSourceGenerators(ReferenceAssemblies referenceAssemblies)
    {
        var package = referenceAssemblies.ReferenceAssemblyPackage
            ?? referenceAssemblies.Packages.FirstOrDefault(package => package.Id is "Microsoft.NETCore.App.Ref")
            ?? throw new InvalidOperationException($"'{referenceAssemblies.TargetFramework}' does not reference the .NET reference pack");

        var version = package.Version;

        return FrameworkSourceGenerators.GetOrAdd(version, static version =>
        {
            var loader = new GeneratorAssemblyLoader(version);
            return
            [
                .. NuGetPackages.GetReferencesAsync("Microsoft.NETCore.App.Ref", version, ["analyzers/dotnet/cs/"]).Result
                    .SelectMany(path => loader.Load(path).GetTypes())
                    .Where(type => !type.IsAbstract && type.GetCustomAttribute<GeneratorAttribute>() is not null),
            ];
        });
    }

    private static readonly ConcurrentDictionary<string, Type[]> FrameworkSourceGenerators = new(StringComparer.Ordinal);

    /// <summary>
    /// The tests compile with <see cref="MetadataImportOptions.All"/>, which the rules
    /// that analyze the non-public members of the referenced assemblies rely on.
    /// </summary>
    public static Solution ConfigureCompilationOptions(Solution solution, ProjectId projectId)
    {
        var compilationOptions = (CSharpCompilationOptions)solution.GetProject(projectId)!.CompilationOptions!;
        return solution.WithProjectCompilationOptions(projectId, compilationOptions.WithMetadataImportOptions(MetadataImportOptions.All));
    }
}

using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using MetadataReference = Microsoft.CodeAnalysis.MetadataReference;

namespace Meziantou.Analyzer.Test.Harness;

/// <summary>
/// The external analyzers a test can run alongside the analyzer under test, which the tests of the
/// <see cref="Microsoft.CodeAnalysis.Diagnostics.DiagnosticSuppressor"/> rely on to produce the diagnostics they suppress.
/// </summary>
internal static class AnalyzerTestExtensions
{
    /// <summary>
    /// Runs the analyzers of the <c>Microsoft.CodeAnalysis.NetAnalyzers</c> package reporting the given rules,
    /// </summary>
    public static void AddMicrosoftCodeAnalysisNetAnalyzers<TAnalyzer>(this CSharpAnalyzerTest<TAnalyzer> test, params string[] ruleIds)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
#if ROSLYN_5_0_OR_GREATER
        test.AddAnalyzersFromNuGet("Microsoft.CodeAnalysis.NetAnalyzers", "10.0.100", ["analyzers/dotnet/cs/", "analyzers/dotnet/Microsoft."], ruleIds);
#else
        test.AddAnalyzersFromNuGet("Microsoft.CodeAnalysis.NetAnalyzers", "9.0.0", ["analyzers/dotnet/cs/Microsoft.CodeAnalysis"], ruleIds);
#endif
    }

    /// <summary>
    /// Runs the analyzers of the <c>Microsoft.CodeAnalysis.CSharp.CodeStyle</c> package reporting the given rules,
    /// </summary>
    public static void AddMicrosoftCodeAnalysisCSharpCodeStyleAnalyzers<TAnalyzer>(this CSharpAnalyzerTest<TAnalyzer> test, params string[] ruleIds)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        // The IDE analyzers use the async interfaces, which the target framework of the test code does not provide.
        foreach (var reference in NuGetPackages.GetReferencesAsync("Microsoft.Bcl.AsyncInterfaces", "9.0.7", ["lib/netstandard2.1/"]).Result)
        {
            test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(reference));
        }

#if ROSLYN_5_9_OR_GREATER
        test.AddAnalyzersFromNuGet("Microsoft.CodeAnalysis.CSharp.CodeStyle", "5.9.0", ["analyzers/dotnet/cs/", "analyzers/dotnet/Microsoft.CodeAnalysis"], ruleIds);
#elif ROSLYN_5_6_OR_GREATER
        test.AddAnalyzersFromNuGet("Microsoft.CodeAnalysis.CSharp.CodeStyle", "5.6.0", ["analyzers/dotnet/cs/", "analyzers/dotnet/Microsoft.CodeAnalysis"], ruleIds);
#elif ROSLYN_5_0_OR_GREATER
        test.AddAnalyzersFromNuGet("Microsoft.CodeAnalysis.CSharp.CodeStyle", "5.0.0", ["analyzers/dotnet/cs/", "analyzers/dotnet/Microsoft.CodeAnalysis"], ruleIds);
#else
        test.AddAnalyzersFromNuGet("Microsoft.CodeAnalysis.CSharp.CodeStyle", "4.14.0", ["analyzers/dotnet/cs/"], ruleIds);
#endif
    }

    private static void AddAnalyzersFromNuGet<TAnalyzer>(this CSharpAnalyzerTest<TAnalyzer> test, string packageName, string version, string[] paths, string[] ruleIds)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var ruleFound = false;
        foreach (var reference in NuGetPackages.GetReferencesAsync(packageName, version, paths).Result)
        {
            foreach (var type in Assembly.LoadFrom(reference).GetTypes())
            {
                if (type.IsAbstract || !typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
                    continue;

                var instance = (DiagnosticAnalyzer)Activator.CreateInstance(type)!;
                if (instance.SupportedDiagnostics.Any(diagnostic => ruleIds.Contains(diagnostic.Id, StringComparer.Ordinal)))
                {
                    test.AdditionalAnalyzers.Add(instance);
                    ruleFound = true;
                }
            }
        }

        if (!ruleFound)
            throw new InvalidOperationException($"No analyzer of '{packageName}' reports {string.Join(", ", ruleIds)}");
    }
}

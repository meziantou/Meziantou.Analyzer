using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using TestingDiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;

namespace TestHelper;

public sealed partial class ProjectBuilder
{
    public async Task ValidateAsync()
    {
        if (DiagnosticAnalyzer.Count == 0)
        {
            Assert.Fail("DiagnosticAnalyzer is not configured");
        }

        if (ExpectedFixedCode is not null && CodeFixProvider is null)
        {
            Assert.Fail("CodeFixProvider is not configured");
        }

        AddTargetFrameworkReferences();

        // The Roslyn test framework validates the code under test and the fixed code with the same setting, so a
        // test whose fixed code does not compile needs a first run that only validates the code under test
        if (ExpectedFixedCode is not null && IsValidCode && !IsValidFixCode)
        {
            await new ProjectBuilderTest(this, includeCodeFix: false).RunAsync(CancellationToken.None).ConfigureAwait(false);
        }

        await new ProjectBuilderTest(this).RunAsync(CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// The sources of the project, the first one being the code under test.
    /// </summary>
    internal IEnumerable<(string FileName, string Content)> GetSources()
    {
        var count = 0;
        yield return (FileName ?? GetDefaultFileName(count++), SourceCode);

        foreach (var source in ApiReferences)
        {
            yield return (GetDefaultFileName(count++), source);
        }

        static string GetDefaultFileName(int index) => "Test" + index.ToString(CultureInfo.InvariantCulture) + ".cs";
    }

    /// <summary>
    /// Converts the diagnostics declared by the test, either with the <c>[|code|]</c> and <c>{|ruleId:code|}</c>
    /// syntaxes or explicitly, to the <see cref="TestingDiagnosticResult"/> of the Roslyn test framework.
    /// </summary>
    internal ImmutableArray<TestingDiagnosticResult> GetExpectedDiagnostics(ImmutableArray<DiagnosticAnalyzer> analyzers)
    {
        var descriptors = new Dictionary<string, DiagnosticDescriptor>(StringComparer.Ordinal);
        foreach (var descriptor in analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics))
        {
            _ = descriptors.TryAdd(descriptor.Id, descriptor);
        }

        // The framework always validates the id of a diagnostic, so the tests that do not name a rule report the
        // rule of the analyzer under test
        var defaultId = DefaultAnalyzerId ?? FallbackAnalyzerId ?? analyzers.FirstOrDefault(analyzer => analyzer.SupportedDiagnostics.Length > 0)?.SupportedDiagnostics[0].Id;

        var results = ImmutableArray.CreateBuilder<TestingDiagnosticResult>(ExpectedDiagnosticResults.Count);
        foreach (var expected in ExpectedDiagnosticResults)
        {
            var id = expected.Id ?? defaultId ?? throw new InvalidOperationException("The analyzers do not support any diagnostic, so the expected diagnostic id cannot be inferred");
            _ = descriptors.TryGetValue(id, out var descriptor);

            var result = new TestingDiagnosticResult(id, expected.Severity ?? descriptor?.DefaultSeverity ?? DiagnosticSeverity.Warning);
            if (expected.Locations.Count == 0)
            {
                result = result.WithNoLocation();
            }
            else
            {
                foreach (var location in expected.Locations)
                {
                    result = location.IsSpan
                        ? result.WithSpan(location.Path, location.LineStart, location.ColumnStart, location.LineEnd, location.ColumnEnd)
                        : result.WithLocation(location.Path, new LinePosition(location.LineStart - 1, location.ColumnStart - 1));
                }
            }

            if (expected.Message is not null)
            {
                result = result.WithMessage(expected.Message);
            }

            results.Add(result);
        }

        return results.DrainToImmutable();
    }

    private void AddTargetFrameworkReferences()
    {
        switch (TargetFramework)
        {
            case TargetFramework.NetStandard2_0:
                AddNuGetReference("NETStandard.Library", "2.0.3", "build/netstandard2.0/ref/");
                break;

            case TargetFramework.NetStandard2_1:
                AddNuGetReference("NETStandard.Library.Ref", "2.1.0", "ref/netstandard2.1/");
                break;

            case TargetFramework.Net4_8:
                AddNuGetReference("Microsoft.NETFramework.ReferenceAssemblies.net48", "1.0.0", "build/.NETFramework/v4.8/");
                break;

            case TargetFramework.Net5_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "5.0.0", "ref/net5.0/");
                break;

            case TargetFramework.Net6_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "6.0.10", "ref/net6.0/");
                break;

            case TargetFramework.Net7_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "7.0.0", "ref/net7.0/");
                break;

            case TargetFramework.Net8_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "8.0.0", "ref/net8.0/");
                break;

            case TargetFramework.Net9_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "9.0.0", "ref/net9.0/");
                break;

            case TargetFramework.Net10_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "10.0.0", "ref/net10.0/");
                break;

            case TargetFramework.Net11_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "11.0.0-preview.7.26381.103", "ref/net11.0/");
                break;

            case TargetFramework.AspNetCore5_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "5.0.0", "ref/net5.0/");
                AddNuGetReference("Microsoft.AspNetCore.App.Ref", "5.0.0", "ref/net5.0/");
                break;

            case TargetFramework.AspNetCore6_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "6.0.10", "ref/net6.0/");
                AddNuGetReference("Microsoft.AspNetCore.App.Ref", "6.0.10", "ref/net6.0/");
                break;

            case TargetFramework.AspNetCore7_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "7.0.0", "ref/net7.0/");
                AddNuGetReference("Microsoft.AspNetCore.App.Ref", "7.0.0", "ref/net7.0/");
                break;

            case TargetFramework.AspNetCore8_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "8.0.0", "ref/net8.0/");
                AddNuGetReference("Microsoft.AspNetCore.App.Ref", "8.0.0", "ref/net8.0/");
                break;

            case TargetFramework.AspNetCore9_0:
                AddNuGetReference("Microsoft.NETCore.App.Ref", "9.0.0", "ref/net9.0/");
                AddNuGetReference("Microsoft.AspNetCore.App.Ref", "9.0.0", "ref/net9.0/");
                break;

            case TargetFramework.WindowsDesktop5_0:
                AddNuGetReference("Microsoft.WindowsDesktop.App.Ref", "5.0.0", "ref/net5.0/");
                break;
        }

        if (UseFrameworkSourceGenerators)
        {
            switch (TargetFramework)
            {
                case TargetFramework.Net7_0:
                case TargetFramework.AspNetCore7_0:
                    WithSourceGeneratorsFromNuGet("Microsoft.NETCore.App.Ref", "7.0.0", "analyzers/dotnet/cs/");
                    break;

                case TargetFramework.Net8_0:
                case TargetFramework.AspNetCore8_0:
                    WithSourceGeneratorsFromNuGet("Microsoft.NETCore.App.Ref", "8.0.0", "analyzers/dotnet/cs/");
                    break;

                case TargetFramework.AspNetCore9_0:
                case TargetFramework.Net9_0:
                    WithSourceGeneratorsFromNuGet("Microsoft.NETCore.App.Ref", "9.0.0", "analyzers/dotnet/cs/");
                    break;

                case TargetFramework.Net10_0:
                    WithSourceGeneratorsFromNuGet("Microsoft.NETCore.App.Ref", "10.0.0", "analyzers/dotnet/cs/");
                    break;

                case TargetFramework.Net11_0:
                    WithSourceGeneratorsFromNuGet("Microsoft.NETCore.App.Ref", "11.0.0-preview.4.26230.115", "analyzers/dotnet/cs/");
                    break;
            }
        }

        if (TargetFramework is not TargetFramework.Net7_0 and not TargetFramework.Net8_0 and not TargetFramework.Net9_0 and not TargetFramework.Net10_0 and not TargetFramework.Net11_0 and not TargetFramework.AspNetCore9_0)
        {
            AddNuGetReference("System.Collections.Immutable", "1.5.0", "lib/netstandard2.0/");
            AddNuGetReference("System.Numerics.Vectors", "4.5.0", "ref/netstandard2.0/");
        }

        AddNuGetReference("Microsoft.CSharp", "4.7.0", "lib/netstandard2.0/");  // To support dynamic type
    }
}

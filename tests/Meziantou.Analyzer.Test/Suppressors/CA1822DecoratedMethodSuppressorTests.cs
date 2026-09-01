#if ROSLYN_4_10_OR_GREATER
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Suppressors.CA1822DecoratedMethodSuppressor>;

namespace Meziantou.Analyzer.Test.Suppressors;

public sealed class CA1822DecoratedMethodSuppressorTests
{
    /// <summary>
    /// The diagnostic CA1822 reports on the member marked with <c>{|#N:code|}</c>,
    /// which the suppressor is expected to suppress or not.
    /// </summary>
    private static DiagnosticResult CA1822(int location, bool suppressed) =>
        new DiagnosticResult("CA1822", DiagnosticSeverity.Info).WithLocation(location).WithIsSuppressed(suppressed);

    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.AddMicrosoftCodeAnalysisNetAnalyzers("CA1822");
        return test;
    }

    [Fact]
    public Task CA1822IsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class A
            {
                internal void {|#0:Sample|}()
                {
                }

                internal string {|#1:Dummy|} => "";
            }
            """;
        test.ExpectedDiagnostics.Add(CA1822(0, suppressed: false));
        test.ExpectedDiagnostics.Add(CA1822(1, suppressed: false));

        return test.RunAsync();
    }

    [Fact]
    public Task CA1822IsSuppressOnBenchmarkAttributeMethods()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddBenchmarkDotNet();
        test.TestCode = """
            internal class A
            {
                [BenchmarkDotNet.Attributes.BenchmarkAttribute]
                internal void {|#0:Benchmark|}()
                {
                }
            }
            """;
        test.ExpectedDiagnostics.Add(CA1822(0, suppressed: true));

        return test.RunAsync();
    }

    [Fact]
    public Task CA1822IsSuppressOnJsonPropertyNameAttribute()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = """
            internal sealed class Sample
            {
                [System.Text.Json.Serialization.JsonPropertyName("@type")]
                public string? {|#0:Type|} => "ImageObject"; // CA1822 Member 'Type' does not access instance data and can be marked as static

                public string? Id { get; set; }
            }
            """;
        test.ExpectedDiagnostics.Add(CA1822(0, suppressed: true));

        return test.RunAsync();
    }
}
#endif

#if ROSLYN_4_10_OR_GREATER
using System.Threading.Tasks;
using Meziantou.Analyzer.Suppressors;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Suppressors;
public sealed class CA1822DecoratedMethodSuppressorTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithMicrosoftCodeAnalysisNetAnalyzers("CA1822")
            .WithAnalyzer<CA1822DecoratedMethodSuppressor>();
    }

    [Fact]
    public async Task CA1822IsReported()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
internal class A
{
    internal void [||]Sample()
    {
    }

    internal string [||]Dummy => "";
}
""")
            .ValidateAsync();
    }

    [Fact]
    public async Task CA1822IsSuppressOnBenchmarkAttributeMethods()
    {
        await CreateProjectBuilder()
            .AddNuGetReference("BenchmarkDotNet.Annotations", "0.13.5", "lib/netstandard2.0/")
            .WithSourceCode("""
internal class A
{
    [BenchmarkDotNet.Attributes.BenchmarkAttribute]
    internal void Benchmark()
    {
    }
}
""")
            .ValidateAsync();
    }

    [Fact]
    public async Task CA1822IsSuppressOnJsonPropertyNameAttribute()
    {
        await CreateProjectBuilder()
            .WithTargetFramework(TargetFramework.Net7_0)
            .WithSourceCode("""
internal sealed class Sample
{
    [System.Text.Json.Serialization.JsonPropertyName("@type")]
    public string? Type => "ImageObject"; // CA1822 Member 'Type' does not access instance data and can be marked as static

    public string? Id { get; set; }
}

""")
            .ValidateAsync();
    }
}
#endif
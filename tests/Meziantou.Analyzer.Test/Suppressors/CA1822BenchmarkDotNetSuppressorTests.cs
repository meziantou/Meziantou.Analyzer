using System.Threading.Tasks;
using Meziantou.Analyzer.Suppressors;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Suppressors;
public sealed class CA1822BenchmarkDotNetSuppressorTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .AddNuGetReference("BenchmarkDotNet.Annotations", "0.13.5", "lib/netstandard2.0/")
            .WithAnalyzerFromNuGet("Microsoft.CodeAnalysis.NetAnalyzers", "7.0.1", paths: new[] { "analyzers/dotnet/cs/Microsoft.CodeAnalysis" }, ruleIds: new[] { "CA1822" })
            .WithAnalyzer<CA1822BenchmarkDotNetSuppressor>();
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
}
""")
            .ValidateAsync();
    }

    [Fact]
    public async Task CA1822IsSuppressOnBenchmarkAttributeMethods()
    {
        await CreateProjectBuilder()
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
}

using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseTimeSpanZeroAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseTimeSpanZeroAnalyzer>()
            .WithCodeFixProvider<UseTimeSpanZeroFixer>()
            .WithTargetFramework(Helpers.TargetFramework.NetLatest);
    }

    [Theory]
    [InlineData("System.TimeSpan.FromSeconds(0)")]
    [InlineData("System.TimeSpan.FromSeconds(0.0)")]
    [InlineData("System.TimeSpan.FromMinutes(0)")]
    [InlineData("System.TimeSpan.FromMinutes(0.0)")]
    [InlineData("System.TimeSpan.FromHours(0)")]
    [InlineData("System.TimeSpan.FromHours(0.0)")]
    [InlineData("System.TimeSpan.FromDays(0)")]
    [InlineData("System.TimeSpan.FromDays(0.0)")]
    [InlineData("System.TimeSpan.FromMilliseconds(0)")]
    [InlineData("System.TimeSpan.FromMilliseconds(0L)")]
    [InlineData("System.TimeSpan.FromMilliseconds(0L, 0L)")]
    [InlineData("System.TimeSpan.FromMilliseconds(0.0)")]
    [InlineData("System.TimeSpan.FromMilliseconds(0.0d)")]
    [InlineData("System.TimeSpan.FromMicroseconds(0)")]
    [InlineData("System.TimeSpan.FromMicroseconds(0.0)")]
    [InlineData("System.TimeSpan.FromTicks(0)")]
    [InlineData("System.TimeSpan.FromTicks(0L)")]
    public async Task ShouldReportDiagnostic(string code)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
class TestClass
{
    void Test()
    {
        _ = [||]{{code}};
    }
}
""")
              .ShouldFixCodeWith("""
class TestClass
{
    void Test()
    {
        _ = System.TimeSpan.Zero;
    }
}
""")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("System.TimeSpan.FromSeconds(1)")]
    [InlineData("System.TimeSpan.FromSeconds(0.5)")]
    [InlineData("System.TimeSpan.FromMinutes(1)")]
    [InlineData("System.TimeSpan.FromHours(1)")]
    [InlineData("System.TimeSpan.FromDays(1)")]
    [InlineData("System.TimeSpan.FromMilliseconds(100)")]
    [InlineData("System.TimeSpan.FromMicroseconds(1)")]
    [InlineData("System.TimeSpan.FromTicks(1)")]
    [InlineData("System.TimeSpan.Zero")]
    [InlineData("new System.TimeSpan()")]
    [InlineData("new System.TimeSpan(0)")]
    [InlineData("default(System.TimeSpan)")]
    public async Task ShouldNotReportDiagnostic(string code)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
class TestClass
{
    void Test()
    {
        _ = {{code}};
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task ShouldReportDiagnostic_MultipleOccurrences()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
class TestClass
{
    void Test()
    {
        _ = [||]System.TimeSpan.FromSeconds(0);
    }
}
""")
              .ShouldFixCodeWith("""
class TestClass
{
    void Test()
    {
        _ = System.TimeSpan.Zero;
    }
}
""")
              .ValidateAsync();
    }
}

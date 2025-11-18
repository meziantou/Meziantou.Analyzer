using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseTimeSpanZeroAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseTimeSpanZeroAnalyzer>()
            .WithCodeFixProvider<UseTimeSpanZeroFixer>();
    }

    [Theory]
    [InlineData("TimeSpan.FromSeconds(0)")]
    [InlineData("TimeSpan.FromSeconds(0.0)")]
    [InlineData("TimeSpan.FromMinutes(0)")]
    [InlineData("TimeSpan.FromMinutes(0.0)")]
    [InlineData("TimeSpan.FromHours(0)")]
    [InlineData("TimeSpan.FromHours(0.0)")]
    [InlineData("TimeSpan.FromDays(0)")]
    [InlineData("TimeSpan.FromDays(0.0)")]
    [InlineData("TimeSpan.FromMilliseconds(0)")]
    [InlineData("TimeSpan.FromMilliseconds(0.0)")]
    [InlineData("TimeSpan.FromMicroseconds(0)")]
    [InlineData("TimeSpan.FromMicroseconds(0.0)")]
    [InlineData("TimeSpan.FromTicks(0)")]
    [InlineData("TimeSpan.FromTicks(0L)")]
    [InlineData("System.TimeSpan.FromSeconds(0)")]
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
    [InlineData("TimeSpan.FromSeconds(1)")]
    [InlineData("TimeSpan.FromSeconds(0.5)")]
    [InlineData("TimeSpan.FromMinutes(1)")]
    [InlineData("TimeSpan.FromHours(1)")]
    [InlineData("TimeSpan.FromDays(1)")]
    [InlineData("TimeSpan.FromMilliseconds(100)")]
    [InlineData("TimeSpan.FromMicroseconds(1)")]
    [InlineData("TimeSpan.FromTicks(1)")]
    [InlineData("TimeSpan.Zero")]
    [InlineData("new TimeSpan()")]
    [InlineData("new TimeSpan(0)")]
    [InlineData("default(TimeSpan)")]
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
        _ = [||]TimeSpan.FromSeconds(0);
        _ = [||]TimeSpan.FromMinutes(0);
        _ = [||]TimeSpan.FromHours(0);
    }
}
""")
              .ShouldFixCodeWith("""
class TestClass
{
    void Test()
    {
        _ = System.TimeSpan.Zero;
        _ = System.TimeSpan.Zero;
        _ = System.TimeSpan.Zero;
    }
}
""")
              .ValidateAsync();
    }
}

using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseGuidEmptyAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseGuidEmptyAnalyzer>()
            .WithCodeFixProvider<UseGuidEmptyFixer>();
    }

    [Theory]
    [InlineData("new System.Guid()")]
    [InlineData("new System.Guid(\"00000000-0000-0000-0000-000000000000\")")]
    [InlineData("new System.Guid(\"{00000000-0000-0000-0000-000000000000}\")")]
    [InlineData("new System.Guid(\"00000000000000000000000000000000\")")]
    [InlineData("new System.Guid(\"(00000000-0000-0000-0000-000000000000)\")")]
    [InlineData("new System.Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)")]
    [InlineData("System.Guid.Parse(\"00000000-0000-0000-0000-000000000000\")")]
    [InlineData("System.Guid.Parse(\"{00000000-0000-0000-0000-000000000000}\")")]
    [InlineData("System.Guid.Parse(\"00000000000000000000000000000000\")")]
    [InlineData("System.Guid.Parse(\"(00000000-0000-0000-0000-000000000000)\")")]
    public async Task ShouldReportError(string code)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
class TestClass
{
    void Test()
    {
        _ = [|{{code}}|];
    }
}
""")
              .ShouldFixCodeWith("""
class TestClass
{
    void Test()
    {
        _ = System.Guid.Empty;
    }
}
""")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("new System.Guid(\"\")")]
    [InlineData("new System.Guid(\"10752bc4-c151-50f5-f27b-df92d8af5a61\")")]
    [InlineData("System.Guid.Parse(\"10752bc4-c151-50f5-f27b-df92d8af5a61\")")]
    [InlineData("new System.Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)")]
    public async Task ShouldNotReportError(string code)
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
}

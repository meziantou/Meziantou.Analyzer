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
    public async Task ShouldReportError(string code)
    {
        await CreateProjectBuilder()
              .WithSourceCode($@"
class TestClass
{{
    void Test()
    {{
        _ = [||]{code};
    }}
}}")
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
    public async Task ShouldNotReportError(string code)
    {
        await CreateProjectBuilder()
              .WithSourceCode($@"
class TestClass
{{
    void Test()
    {{
        _ = {code};
    }}
}}")
              .ValidateAsync();
    }
}

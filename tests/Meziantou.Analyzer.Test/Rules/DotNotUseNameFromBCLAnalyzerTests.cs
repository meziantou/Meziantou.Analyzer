using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DotNotUseNameFromBCLAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DotNotUseNameFromBCLAnalyzer>();
    }

    [Theory]
    [InlineData("Action")]
    [InlineData("Action<T>")]
    [InlineData("Func<T1, T2>")]
    [InlineData("String")]
    public async Task ReportDiagnostic(string typeName)
    {
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration("MA0104.use_preview_types", "true")
              .WithSourceCode("public class [||]" + typeName + " { }")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("Action")]
    [InlineData("Action<T>")]
    [InlineData("Func<T1, T2>")]
    [InlineData("String")]
    public async Task ReportDiagnostic_UsePreviewTypes(string typeName)
    {
        await CreateProjectBuilder()
              .WithSourceCode("public class [||]" + typeName + " { }")
              .ValidateAsync();
    }

    [Fact]
    public async Task DoNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  public class Dummy { }")
                                .ValidateAsync();
                      }
                  
                      [Fact]
                      public async Task NestedType_DoNotReportDiagnostic()
                      {
                          await CreateProjectBuilder()
                                .WithSourceCode("""
                                    public class Dummy { public class Action { } }
                                                      ""
                                    """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Regex_DoNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  public class Action { }")
                                .AddAnalyzerConfiguration("MA0104.namespaces_regex", "dummy
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Regex_DoNotReportDiagnostic_OldConfigurationName()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  public class Action { }")
                                .AddAnalyzerConfiguration("MA0104.namepaces_regex", "dummy
                  """)
              .ValidateAsync();
    }
}

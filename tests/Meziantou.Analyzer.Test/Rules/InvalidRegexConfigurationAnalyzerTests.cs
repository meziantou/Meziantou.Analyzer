namespace Meziantou.Analyzer.Test.Rules;

public sealed class InvalidRegexConfigurationAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<InvalidRegexConfigurationAnalyzer>();
    }

    [Fact]
    public async Task NoConfiguration_DoNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("class Test { }")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("MA0003.excluded_methods_regex")]
    [InlineData("MA0104.namespaces_regex")]
    [InlineData("MA0104.namepaces_regex")]
    public async Task ValidRegex_DoNotReportDiagnostic(string key)
    {
        await CreateProjectBuilder()
              .WithSourceCode("class Test { }")
              .AddAnalyzerConfiguration(key, "^System($|\\.)")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("MA0003.excluded_methods_regex")]
    [InlineData("MA0104.namespaces_regex")]
    [InlineData("MA0104.namepaces_regex")]
    public async Task InvalidRegex_ReportDiagnostic(string key)
    {
        await CreateProjectBuilder()
              .WithSourceCode("class Test { }")
              .AddAnalyzerConfiguration(key, "[")
              .ShouldReportDiagnostic(new DiagnosticResult { Id = "MA0220" })
              .ValidateAsync();
    }

    [Fact]
    public async Task InvalidRegex_ReportDiagnosticOnlyOnceForMultipleFiles()
    {
        var builder = CreateProjectBuilder()
              .WithSourceCode("class Test1 { }")
              .AddAnalyzerConfiguration("MA0104.namespaces_regex", "[")
              .ShouldReportDiagnostic(new DiagnosticResult { Id = "MA0220" });
        builder.ApiReferences.Add("class Test2 { }");

        await builder.ValidateAsync();
    }

    [Fact]
    public async Task MultipleInvalidRegex_ReportOneDiagnosticPerConfiguration()
    {
        await CreateProjectBuilder()
              .WithSourceCode("class Test { }")
              .AddAnalyzerConfiguration("MA0003.excluded_methods_regex", "[")
              .AddAnalyzerConfiguration("MA0104.namespaces_regex", "(")
              .ShouldReportDiagnostic(new DiagnosticResult { Id = "MA0220" }, new DiagnosticResult { Id = "MA0220" })
              .ValidateAsync();
    }
}

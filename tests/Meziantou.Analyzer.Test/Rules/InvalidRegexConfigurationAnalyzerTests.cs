using Microsoft.CodeAnalysis;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.InvalidRegexConfigurationAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class InvalidRegexConfigurationAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    /// <summary>
    /// The rule reports the invalid configuration values of the whole compilation, so the diagnostic has no location.
    /// </summary>
    private static DiagnosticResult ExpectedInvalidRegex() =>
        new(RuleIdentifiers.InvalidRegexConfiguration, DiagnosticSeverity.Warning);

    [Fact]
    public Task NoConfiguration_DoNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = "class Test { }";

        return test.RunAsync();
    }

    [Theory]
    [InlineData("MA0003.excluded_methods_regex")]
    [InlineData("MA0104.namespaces_regex")]
    [InlineData("MA0104.namepaces_regex")]
    public Task ValidRegex_DoNotReportDiagnostic(string key)
    {
        var test = CreateTest();
        test.TestCode = "class Test { }";
        test.TestState.SetConfiguration(key, "^System($|\\.)");

        return test.RunAsync();
    }

    [Theory]
    [InlineData("MA0003.excluded_methods_regex")]
    [InlineData("MA0104.namespaces_regex")]
    [InlineData("MA0104.namepaces_regex")]
    public Task InvalidRegex_ReportDiagnostic(string key)
    {
        var test = CreateTest();
        test.TestCode = "class Test { }";
        test.TestState.SetConfiguration(key, "[");
        test.ExpectedDiagnostics.Add(ExpectedInvalidRegex());

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidRegex_ReportDiagnosticOnlyOnceForMultipleFiles()
    {
        var test = CreateTest();
        test.TestCode = "class Test1 { }";
        test.TestState.Sources.Add("class Test2 { }");
        test.TestState.SetConfiguration("MA0104.namespaces_regex", "[");
        test.ExpectedDiagnostics.Add(ExpectedInvalidRegex());

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleInvalidRegex_ReportOneDiagnosticPerConfiguration()
    {
        var test = CreateTest();
        test.TestCode = "class Test { }";
        test.TestState.SetConfiguration(("MA0003.excluded_methods_regex", "["), ("MA0104.namespaces_regex", "("));
        test.ExpectedDiagnostics.Add(ExpectedInvalidRegex());
        test.ExpectedDiagnostics.Add(ExpectedInvalidRegex());

        return test.RunAsync();
    }
}

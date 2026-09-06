using System.Reflection;
using Meziantou.Analyzer.Configurations;
using Meziantou.Analyzer.Rules;
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
    [InlineData("MA0048.excluded_file_name_parts_regex")]
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
    [InlineData("MA0048.excluded_file_name_parts_regex")]
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

    /// <summary>
    /// The rule validates the options discovered from the assembly, so an option whose value is a regular expression
    /// must set its <see cref="ConfigurationDefinition{T}.RegexOptions"/> to be validated by this rule.
    /// </summary>
    [Fact]
    public void AllOptionsEndingWithRegexAreValidatedByTheRule()
    {
        // The legacy names of an option are validated too, so every key of a definition is considered
        var validatedKeys = InvalidRegexConfigurationAnalyzer.GetRegexConfigurations().SelectMany(configuration => configuration.Keys).ToArray();

        var missingKeys = new List<string>();
        foreach (var type in typeof(InvalidRegexConfigurationAnalyzer).Assembly.GetTypes())
        {
            foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.FieldType != typeof(ConfigurationDefinition<string>))
                    continue;

                if (field.GetValue(null) is ConfigurationDefinition<string> { IsRegex: false } configuration)
                {
                    missingKeys.AddRange(configuration.Keys.Where(key => key.EndsWith("_regex", StringComparison.Ordinal)));
                }
            }
        }

        Assert.Empty(missingKeys);
        Assert.Contains("MA0048.excluded_file_name_parts_regex", validatedKeys, StringComparer.Ordinal);
        Assert.Contains("MA0003.excluded_methods_regex", validatedKeys, StringComparer.Ordinal);
        Assert.Contains("MA0104.namespaces_regex", validatedKeys, StringComparer.Ordinal);
        Assert.Contains("MA0104.namepaces_regex", validatedKeys, StringComparer.Ordinal);
    }
}

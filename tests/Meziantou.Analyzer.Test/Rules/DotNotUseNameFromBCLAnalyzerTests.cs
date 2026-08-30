using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DotNotUseNameFromBCLAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DotNotUseNameFromBCLAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    private static string MarkTypeName(string typeName)
    {
        var genericStart = typeName.IndexOf('<', StringComparison.Ordinal);
        return genericStart >= 0
            ? "[|" + typeName[..genericStart] + "|]" + typeName[genericStart..]
            : "[|" + typeName + "|]";
    }

    [Theory]
    [InlineData("Action")]
    [InlineData("Action<T>")]
    [InlineData("Func<T1, T2>")]
    [InlineData("String")]
    public Task ReportDiagnostic(string typeName)
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0104.use_preview_types", "true");
        test.TestCode = "public class " + MarkTypeName(typeName) + " { }";

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Action")]
    [InlineData("Action<T>")]
    [InlineData("Func<T1, T2>")]
    [InlineData("String")]
    public Task ReportDiagnostic_UsePreviewTypes(string typeName)
    {
        var test = CreateTest();
        test.TestCode = "public class " + MarkTypeName(typeName) + " { }";

        return test.RunAsync();
    }

    [Fact]
    public Task DoNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = "public class Dummy { }";

        return test.RunAsync();
    }

    [Fact]
    public Task NestedType_DoNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = "public class Dummy { public class Action { } }";

        return test.RunAsync();
    }

    [Fact]
    public Task Regex_DoNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0104.namespaces_regex", "dummy");
        test.TestCode = "public class Action { }";

        return test.RunAsync();
    }

    [Fact]
    public Task Regex_DoNotReportDiagnostic_OldConfigurationName()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0104.namepaces_regex", "dummy");
        test.TestCode = "public class Action { }";

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidRegex_UseDefaultRegex()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0104.namespaces_regex", "[");
        test.TestCode = "public class [|Action|] { }";

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidRegex_UseDefaultRegex_OldConfigurationName()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0104.namepaces_regex", "[");
        test.TestCode = "public class [|Action|] { }";

        return test.RunAsync();
    }
}

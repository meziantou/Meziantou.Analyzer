using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.RemoveUselessToStringAnalyzer,
    Meziantou.Analyzer.Rules.RemoveUselessToStringFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RemoveUselessToStringAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task IntToString_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A() => 1.ToString();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringToString_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public string A() => [|"".ToString()|];
            }
            """;
        test.FixedCode = """
            class Test
            {
                public string A() => "";
            }
            """;

        return test.RunAsync();
    }
}

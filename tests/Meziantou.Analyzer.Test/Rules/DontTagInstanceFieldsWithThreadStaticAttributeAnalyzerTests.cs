using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DontTagInstanceFieldsWithThreadStaticAttributeAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DontTagInstanceFieldsWithThreadStaticAttributeAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task DontReport()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test2
            {
                int _a;
                [System.ThreadStatic]
                static int _b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test2
            {
                [System.ThreadStatic]
                int [|_a|];
            }
            """;

        return test.RunAsync();
    }
}

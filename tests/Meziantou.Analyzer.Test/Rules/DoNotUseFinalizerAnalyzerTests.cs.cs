using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseFinalizerAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseFinalizerAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task TestFinalizerReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public Test() { }
                internal void A() { }

                ~{|MA0055:Test|}()
                {
                }
            }
            """;

        return test.RunAsync();
    }
}

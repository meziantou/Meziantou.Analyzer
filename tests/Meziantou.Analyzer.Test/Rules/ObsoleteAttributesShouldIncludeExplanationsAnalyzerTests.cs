using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.ObsoleteAttributesShouldIncludeExplanationsAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ObsoleteAttributesShouldIncludeExplanationsAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task HasMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [System.Obsolete("message")]
                public void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [[|System.Obsolete()|]]
                public void A() { }
            }
            """;

        return test.RunAsync();
    }
}

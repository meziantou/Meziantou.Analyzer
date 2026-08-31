using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.DoNotUseEqualityComparerDefaultOfStringAnalyzer,
    Meziantou.Analyzer.Rules.DoNotUseEqualityComparerDefaultOfStringFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseEqualityComparerDefaultOfStringAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task TestAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class Test
            {
                internal void Sample()
                {
                    _ = EqualityComparer<int>.Default.Equals(0, 0);
                    _ = {|MA0024:EqualityComparer<string>.Default|}.Equals(null, null);
                }
            }
            """;
        test.FixedCode = """
            using System.Collections.Generic;
            class Test
            {
                internal void Sample()
                {
                    _ = EqualityComparer<int>.Default.Equals(0, 0);
                    _ = System.StringComparer.Ordinal.Equals(null, null);
                }
            }
            """;

        return test.RunAsync();
    }
}

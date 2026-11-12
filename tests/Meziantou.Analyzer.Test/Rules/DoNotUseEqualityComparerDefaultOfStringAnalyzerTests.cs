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

    [Fact]
    public Task ComparerDefault_Ordinal()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class Test
            {
                internal void Sample(List<string> list)
                {
                    _ = Comparer<int>.Default.Compare(0, 0);
                    list.Sort({|MA0024:Comparer<string>.Default|});
                }
            }
            """;
        test.FixedCode = """
            using System.Collections.Generic;
            class Test
            {
                internal void Sample(List<string> list)
                {
                    _ = Comparer<int>.Default.Compare(0, 0);
                    list.Sort(System.StringComparer.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ComparerDefault_OrdinalIgnoreCase()
    {
        var test = CreateTest();
        test.CodeActionIndex = 1;
        test.TestCode = """
            using System.Collections.Generic;
            class Test
            {
                internal void Sample()
                {
                    _ = {|MA0024:Comparer<string>.Default|}.Compare("a", "b");
                }
            }
            """;
        test.FixedCode = """
            using System.Collections.Generic;
            class Test
            {
                internal void Sample()
                {
                    _ = System.StringComparer.OrdinalIgnoreCase.Compare("a", "b");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ComparerDefault_Nameof()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class Test
            {
                internal void Sample()
                {
                    _ = nameof(Comparer<string>.Default);
                }
            }
            """;

        return test.RunAsync();
    }
}

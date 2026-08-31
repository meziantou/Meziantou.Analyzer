using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseContainsKeyInsteadOfTryGetValueAnalyzer,
    Meziantou.Analyzer.Rules.UseContainsKeyInsteadOfTryGetValueFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseContainsKeyInsteadOfTryGetValueAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task IDictionary_TryGetValue_Value()
    {
        var test = CreateTest();
        test.TestCode = """
            class ClassTest
            {
                void Test(System.Collections.Generic.IDictionary<string, string> dict)
                {
                    dict.TryGetValue("", out var a);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IDictionary_TryGetValue_Discard()
    {
        var test = CreateTest();
        test.TestCode = """
            class ClassTest
            {
                void Test(System.Collections.Generic.IDictionary<string, string> dict)
                {
                    {|MA0160:dict.TryGetValue("", out _)|};
                }
            }
            """;
        test.FixedCode = """
            class ClassTest
            {
                void Test(System.Collections.Generic.IDictionary<string, string> dict)
                {
                    dict.ContainsKey("");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IReadOnlyDictionary_TryGetValue_Discard()
    {
        var test = CreateTest();
        test.TestCode = """
            class ClassTest
            {
                void Test(System.Collections.Generic.IReadOnlyDictionary<string, string> dict)
                {
                    {|MA0160:dict.TryGetValue("", out _)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Dictionary_TryGetValue_Discard()
    {
        var test = CreateTest();
        test.TestCode = """
            class ClassTest
            {
                void Test(System.Collections.Generic.Dictionary<string, string> dict)
                {
                    {|MA0160:dict.TryGetValue("", out _)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CustomDictionary_TryGetValue_Discard()
    {
        var test = CreateTest();
        test.TestCode = """
            class ClassTest
            {
                void Test(SampleDictionary dict)
                {
                    {|MA0160:dict.TryGetValue("", out _)|};
                }
            }

            class SampleDictionary : System.Collections.Generic.Dictionary<string, string>
            {
            }
            """;

        return test.RunAsync();
    }
}

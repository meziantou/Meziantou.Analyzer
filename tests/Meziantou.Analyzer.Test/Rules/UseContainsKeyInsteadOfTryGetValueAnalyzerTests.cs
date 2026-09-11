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

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    public Task ExplicitContainsKey_TryGetValue_Discard(string kind)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Collections;
            using System.Collections.Generic;

            {{kind}} Map : IReadOnlyDictionary<int, int>
            {
                public int this[int key] => 0;
                public IEnumerable<int> Keys => [];
                public IEnumerable<int> Values => [];
                public int Count => 0;
                bool IReadOnlyDictionary<int, int>.ContainsKey(int key) => false;
                public bool TryGetValue(int key, out int value) { value = 0; return false; }
                public IEnumerator<KeyValuePair<int, int>> GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            class Sample
            {
                bool Run(Map map) => map.TryGetValue(0, out _);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitContainsKey_UnrelatedPublicContainsKey_TryGetValue_Discard()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            class Map : IReadOnlyDictionary<int, int>
            {
                public int this[int key] => 0;
                public IEnumerable<int> Keys => [];
                public IEnumerable<int> Values => [];
                public int Count => 0;
                public bool ContainsKey(int key) => true;
                bool IReadOnlyDictionary<int, int>.ContainsKey(int key) => false;
                public bool TryGetValue(int key, out int value) { value = 0; return false; }
                public IEnumerator<KeyValuePair<int, int>> GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            class Sample
            {
                bool Run(Map map) => map.TryGetValue(0, out _);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitContainsKey_InterfaceReceiver_TryGetValue_Discard()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            class Map : IReadOnlyDictionary<int, int>
            {
                public int this[int key] => 0;
                public IEnumerable<int> Keys => [];
                public IEnumerable<int> Values => [];
                public int Count => 0;
                bool IReadOnlyDictionary<int, int>.ContainsKey(int key) => false;
                public bool TryGetValue(int key, out int value) { value = 0; return false; }
                public IEnumerator<KeyValuePair<int, int>> GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            class Sample
            {
                bool Run(IReadOnlyDictionary<int, int> map) => {|MA0160:map.TryGetValue(0, out _)|};
            }
            """;
        test.FixedCode = """
            using System.Collections;
            using System.Collections.Generic;

            class Map : IReadOnlyDictionary<int, int>
            {
                public int this[int key] => 0;
                public IEnumerable<int> Keys => [];
                public IEnumerable<int> Values => [];
                public int Count => 0;
                bool IReadOnlyDictionary<int, int>.ContainsKey(int key) => false;
                public bool TryGetValue(int key, out int value) { value = 0; return false; }
                public IEnumerator<KeyValuePair<int, int>> GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            class Sample
            {
                bool Run(IReadOnlyDictionary<int, int> map) => map.ContainsKey(0);
            }
            """;

        return test.RunAsync();
    }
}

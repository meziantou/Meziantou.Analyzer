using Microsoft.CodeAnalysis.Testing;
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

    [Fact]
    public Task Dictionary_TryGetValue_Discard_Fix()
    {
        var test = CreateTest();
        test.TestCode = """
            class ClassTest
            {
                bool Test(System.Collections.Generic.Dictionary<string, string> dict) => {|MA0160:dict.TryGetValue("", out _)|};
            }
            """;
        test.FixedCode = """
            class ClassTest
            {
                bool Test(System.Collections.Generic.Dictionary<string, string> dict) => dict.ContainsKey("");
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitContainsKey_Class_CastToInterface()
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
                bool Run(Map map) => {|MA0160:map.TryGetValue(0, out _)|};
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
                bool Run(Map map) => ((IReadOnlyDictionary<int, int>)map).ContainsKey(0);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitContainsKey_NewExpression_CastToInterface()
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
                object Run() => {|MA0160:new Map().TryGetValue(0, out _)|};
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
                object Run() => ((IReadOnlyDictionary<int, int>)new Map()).ContainsKey(0);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitContainsKey_Struct_NoFix()
    {
        // Casting the struct to the interface would box it
        const string Code = """
            using System.Collections;
            using System.Collections.Generic;

            struct Map : IReadOnlyDictionary<int, int>
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
                bool Run(Map map) => {|MA0160:map.TryGetValue(0, out _)|};
            }
            """;

        var test = CreateTest();
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.TestCode = Code;
        test.FixedCode = Code;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitContainsKey_UnrelatedPublicContainsKey_CastToInterface()
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
                bool Run(Map map) => {|MA0160:map.TryGetValue(0, out _)|};
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
                public bool ContainsKey(int key) => true;
                bool IReadOnlyDictionary<int, int>.ContainsKey(int key) => false;
                public bool TryGetValue(int key, out int value) { value = 0; return false; }
                public IEnumerator<KeyValuePair<int, int>> GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            class Sample
            {
                bool Run(Map map) => ((IReadOnlyDictionary<int, int>)map).ContainsKey(0);
            }
            """;

        return test.RunAsync();
    }
}

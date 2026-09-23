using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseValueFactoryWhenUsingConcurrentDictionaryAnalyzer,
    Meziantou.Analyzer.Rules.UseValueFactoryWhenUsingConcurrentDictionaryFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseValueFactoryWhenUsingConcurrentDictionaryAnalyzerTests
{
    [Theory]
    [InlineData("value")]
    [InlineData("(int)floatValue")]
    [InlineData("1")]
    [InlineData("Constant")]
    [InlineData("-value")]
    [InlineData("value + 1")]
    [InlineData("flag ? value : 0")]
    [InlineData("_field")]
    [InlineData("this._field")]
    [InlineData("Property")]
    [InlineData("array[0]")]
    [InlineData("default(int)")]
    [InlineData("sizeof(int)")]
    [InlineData("nullable ?? 0")]
    [InlineData("nullable ?? throw new System.Exception()")]
    [InlineData("new int()")]
    public Task GetOrAdd_CheapValue_NoDiagnostic(string value)
    {
        return new CodeFixTest
        {
            TestCode = $$"""
                using System.Collections.Concurrent;

                class Sample
                {
                    private const int Constant = 1;
                    private int _field;
                    private int Property => 1;

                    void Test(ConcurrentDictionary<string, int> dict, int value, float floatValue, bool flag, int[] array, int? nullable)
                    {
                        dict.GetOrAdd("key", {{value}});
                    }
                }
                """,
        }.RunAsync();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"a\"")]
    [InlineData("\"a\" + \"b\"")]
    [InlineData("nameof(Sample)")]
    [InlineData("$\"{nameof(Sample)}\"")]
    [InlineData("typeof(Sample)")]
    [InlineData("str")]
    [InlineData("(object)str")]
    [InlineData("obj?.Value")]
    public Task GetOrAdd_CheapReferenceValue_NoDiagnostic(string value)
    {
        return new CodeFixTest
        {
            TestCode = $$"""
                using System.Collections.Concurrent;

                class Sample
                {
                    public string Value => "";

                    void Test(ConcurrentDictionary<string, object> dict, string str, Sample obj)
                    {
                        dict.GetOrAdd("key", {{value}});
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_ValueTuple_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, (int, string)> dict, int a, string b)
                    {
                        dict.GetOrAdd("key", (a, b));
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_ValueIsDelegate_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System;
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, Func<int>> dict)
                    {
                        dict.GetOrAdd("key", () => 1);
                        dict.GetOrAdd("key", GetValue);
                    }

                    static int GetValue() => 1;
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_ValueFactory_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, string> dict, object obj)
                    {
                        dict.GetOrAdd("key", _ => obj.ToString());
                        dict.GetOrAdd("key", (_, arg) => arg.ToString(), obj);
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_Invocation()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System.Collections.Concurrent;

                internal sealed class MyClass
                {
                    private readonly ConcurrentDictionary<System.Type, string> _dict = new();

                    public string Invoke(object obj, System.Type type)
                        => _dict.GetOrAdd(type, [|obj.ToString()|]);
                }
                """,
            FixedCode = """
                using System.Collections.Concurrent;

                internal sealed class MyClass
                {
                    private readonly ConcurrentDictionary<System.Type, string> _dict = new();

                    public string Invoke(object obj, System.Type type)
                        => _dict.GetOrAdd(type, _ => obj.ToString());
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_InvocationWithCast()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System;
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, int> dict, int value)
                    {
                        dict.GetOrAdd("3", [|(int)Math.Ceiling(Math.Pow(value, 2))|]);
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, int> dict, int value)
                    {
                        dict.GetOrAdd("3", _ => (int)Math.Ceiling(Math.Pow(value, 2)));
                    }
                }
                """,
        }.RunAsync();
    }

    [Theory]
    [InlineData("new List<int>()")]
    [InlineData("new List<int> { value }")]
    [InlineData("[]")]
    public Task GetOrAdd_Allocation(string value)
    {
        return new CodeFixTest
        {
            TestCode = $$"""
                using System.Collections.Concurrent;
                using System.Collections.Generic;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, List<int>> dict, int value)
                    {
                        dict.GetOrAdd("key", [|{{value}}|]);
                    }
                }
                """,
            FixedCode = $$"""
                using System.Collections.Concurrent;
                using System.Collections.Generic;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, List<int>> dict, int value)
                    {
                        dict.GetOrAdd("key", _ => {{value}});
                    }
                }
                """,
        }.RunAsync();
    }

    [Theory]
    [InlineData("str + \"a\"")]
    [InlineData("$\"{str}\"")]
    [InlineData("str.Length.ToString()")]
    [InlineData("list[0]")]
    [InlineData("new Sample()")]
    public Task GetOrAdd_ExpensiveValue(string value)
    {
        return new CodeFixTest
        {
            TestCode = $$"""
                using System.Collections.Concurrent;
                using System.Collections.Generic;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, object> dict, string str, List<string> list)
                    {
                        dict.GetOrAdd("key", [|{{value}}|]);
                    }
                }
                """,
            FixedCode = $$"""
                using System.Collections.Concurrent;
                using System.Collections.Generic;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, object> dict, string str, List<string> list)
                    {
                        dict.GetOrAdd("key", _ => {{value}});
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_StructWithUserDefinedConstructor()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System.Collections.Concurrent;

                struct MyStruct
                {
                    public MyStruct() { }
                }

                class Sample
                {
                    void Test(ConcurrentDictionary<string, MyStruct> dict)
                    {
                        dict.GetOrAdd("key", [|new MyStruct()|]);
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Concurrent;

                struct MyStruct
                {
                    public MyStruct() { }
                }

                class Sample
                {
                    void Test(ConcurrentDictionary<string, MyStruct> dict)
                    {
                        dict.GetOrAdd("key", _ => new MyStruct());
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_NamedArgument()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, string> dict, object obj)
                    {
                        dict.GetOrAdd(value: [|obj.ToString()|], key: "key");
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, string> dict, object obj)
                    {
                        dict.GetOrAdd(valueFactory: _ => obj.ToString(), key: "key");
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_DiscardNameIsUsed()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, string> dict, object _)
                    {
                        dict.GetOrAdd("key", [|_.ToString()|]);
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, string> dict, object _)
                    {
                        dict.GetOrAdd("key", _1 => _.ToString());
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_ValueIsDelegateReturnedByMethod()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System;
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<int, Func<int, int>> dict)
                    {
                        dict.GetOrAdd(1, [|GetValue()|]);
                    }

                    static Func<int, int> GetValue() => i => i;
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<int, Func<int, int>> dict)
                    {
                        dict.GetOrAdd(1, _ => GetValue());
                    }

                    static Func<int, int> GetValue() => i => i;
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_Await_NoCodeFix()
    {
        const string Source = """
            using System.Collections.Concurrent;
            using System.Threading.Tasks;

            class Sample
            {
                async Task Test(ConcurrentDictionary<string, int> dict)
                {
                    dict.GetOrAdd("key", [|await Task.FromResult(1)|]);
                }
            }
            """;

        return new CodeFixTest
        {
            TestCode = Source,
            FixedCode = Source,
        }.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_RefStruct_NoCodeFix()
    {
        const string Source = """
            using System;
            using System.Collections.Concurrent;

            class Sample
            {
                void Test(ConcurrentDictionary<string, string> dict, ReadOnlySpan<char> span)
                {
                    dict.GetOrAdd("key", [|span.ToString()|]);
                }
            }
            """;

        return new CodeFixTest
        {
            TestCode = Source,
            FixedCode = Source,
        }.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_OutVariableUsedAfterTheInvocation_NoCodeFix()
    {
        const string Source = """
            using System.Collections.Concurrent;

            class Sample
            {
                void Test(ConcurrentDictionary<string, bool> dict, string str)
                {
                    dict.GetOrAdd("key", [|int.TryParse(str, out var number)|]);
                    _ = number;
                }
            }
            """;

        return new CodeFixTest
        {
            TestCode = Source,
            FixedCode = Source,
        }.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_AddValue()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, string> dict, object obj)
                    {
                        dict.AddOrUpdate("key", [|obj.ToString()|], (key, oldValue) => oldValue + obj.ToString());
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, string> dict, object obj)
                    {
                        dict.AddOrUpdate("key", _ => obj.ToString(), (key, oldValue) => oldValue + obj.ToString());
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_CheapAddValue_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, int> dict)
                    {
                        dict.AddOrUpdate("key", 1, (key, oldValue) => oldValue + 1);
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task OtherMethods_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System.Collections.Concurrent;

                class Sample
                {
                    void Test(ConcurrentDictionary<string, string> dict, object obj)
                    {
                        dict.TryAdd("key", obj.ToString());
                        dict["key"] = obj.ToString();
                        dict.TryUpdate("key", obj.ToString(), "");
                    }
                }
                """,
        }.RunAsync();
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.AvoidClosureWhenUsingConcurrentDictionaryAnalyzer,
    Meziantou.Analyzer.Rules.AvoidClosureWhenUsingConcurrentDictionaryFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ConcurrentDictionaryMustPreventClosureWhenAccessingTheKeyAnalyzerTests
{
    [Fact]
    public Task GetOrAdd_IsValid()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var factoryArg = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, (k) => k + 1);
            a.GetOrAdd(key, (k, v) => k + v, factoryArg);
            a.GetOrAdd(key, {|MA0106:(k, v) =>
            {
                key = 2; // ok to write a value
                return key + v; // ok to use the value if it is written
            }|}, factoryArg);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_IsValid()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var value = 1;
            var factoryArg = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(key, value, (k, v) => k + v);
            a.AddOrUpdate(key, (k) => k, (k, v) => k + v + 1);
            a.AddOrUpdate(key, (k, arg) => k + arg, (k, v, arg) => k + v + arg, factoryArg);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var value = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, {|MA0105:k => key|});
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_StringInterpolation()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var value = 1;
            var dict = new ConcurrentDictionary<int, string>();
            dict.GetOrAdd(key, {|MA0105:k => $"{key}"|});
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_StringInterpolation_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var value = 1;
            var dict = new ConcurrentDictionary<int, string>();
            dict.GetOrAdd(key, {|MA0105:k => $"{key}"|});
            """;
        test.FixedCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var value = 1;
            var dict = new ConcurrentDictionary<int, string>();
            dict.GetOrAdd(key, k => $"{k}");
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_Parameter()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Concurrent;

            class Test
            {
                void A(int value)
                {
                    var key = 1;
                    var a = new ConcurrentDictionary<int, int>();
                    a.AddOrUpdate(key, value, {|MA0106:(k, oldValue) => value|});
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_Parameter_IsValid()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Concurrent;

            class Test
            {
                void A(int value)
                {
                    var key = 1;
                    var a = new ConcurrentDictionary<int, int>();
                    a.AddOrUpdate(key, value, (k, v) => k + v);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_Variable_IsValid()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Concurrent;

            class Test
            {
                void A()
                {
                    var key = 1;
                    var value = 1;
                    var a = new ConcurrentDictionary<int, int>();

                    a.AddOrUpdate(key, addValueFactory: k => k, updateValueFactory: {|MA0106:(k, v) => value|});
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_Variable_netstandard2()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestCode = """
            using System.Collections.Concurrent;

            class Test
            {
                void A()
                {
                    var name = 1;
                    var newValue = 1;
                    var concurrentDictionary = new ConcurrentDictionary<int, int>();
                    concurrentDictionary.AddOrUpdate(name, newValue, (key, oldValue) => newValue);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_FactoryArg_IsValid()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var value = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, (k) => k + 1);
            a.GetOrAdd(key, (_, v) => v, value);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_NoOverload_IsValid()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var value = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, (k) => k + 1);
            a.GetOrAdd(key, _ => value);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_TArg_IsValid()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System;
            using System.Collections.Concurrent;

            var key = 1;
            var closure = "";
            var a = new ConcurrentDictionary<int, Func<string>>();
            a.GetOrAdd<Func<string>>(key, (_, v) => v, () => closure);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_Key_IsValid()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System;
            using System.Collections.Concurrent;

            var key = 1;
            var closure = "";
            var a = new ConcurrentDictionary<Func<string>, int>();
            a.GetOrAdd(() => closure, _ => 0);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_Closure()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var value = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, {|MA0106:_ => value|});
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_Closure_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var value = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, {|MA0106:_ => value|});
            """;
        test.FixedCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var value = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, (_, arg) => arg, value);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_ClosureWithLambdaParameter()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, k => new System.Func<int>(() => k)());
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_NoClosure()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System;
            using System.Collections.Concurrent;
            using System.Linq;

            var dict = new ConcurrentDictionary<string, Type>();
            dict.GetOrAdd("", static layout2 =>
            {
                var types = System.Array.Empty<string>().Where(t => t == layout2);
                throw null!;
            });

            var dummy = new object();
            var f = new System.Func<bool>(() => dummy != null);
            """;

        return test.RunAsync();
    }
}

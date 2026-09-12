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
            a.GetOrAdd(key, (k, v) =>
            {
                key = 2; // ok to write a value
                return key + v; // ok to use the value if it is written
            }, factoryArg);
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
    public Task AddOrUpdate_AddValueFactoryUsingTheKey_ReportsOnlyTheLambdaParameterRule()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(key, {|MA0105:k => key|}, (k, v) => k + v);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_AddValueFactoryUsingTheKey_UpdateValueFactoryUsingAClosure()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var value = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(key, {|MA0105:k => key|}, {|MA0106:(k, v) => value|});
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
    public Task AddOrUpdate_Closure_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var value = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(key, k => k, {|MA0106:(k, v) => value|});
            """;
        test.FixedCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var value = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(key, (k, arg) => k, (k, v, arg) => arg, value);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_FactoryArg_UsingTheKeyAndTheFactoryArg_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var factoryArg = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, {|MA0105:(k, arg) => factoryArg|}, factoryArg);
            """;
        test.FixedCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var factoryArg = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, (k, arg) => arg, factoryArg);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_AddValueFactoryUsingTheKey_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(key, {|MA0105:k => key|}, (k, v) => k + v);
            """;
        test.FixedCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(key, k => k, (k, v) => k + v);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_UpdateValueFactoryUsingTheKey_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(key, k => k, {|MA0105:(k, v) => key + v|});
            """;
        test.FixedCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(key, k => k, (k, v) => k + v);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_FactoryArg_AddValueFactoryUsingTheKeyAndTheFactoryArg_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var factoryArg = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(key, {|MA0105:(k, arg) => factoryArg|}, (k, v, arg) => k + v + arg, factoryArg);
            """;
        test.FixedCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var factoryArg = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(key, (k, arg) => arg, (k, v, arg) => k + v + arg, factoryArg);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_FactoryArg_UpdateValueFactoryUsingTheKeyAndTheFactoryArg_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var factoryArg = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(key, (k, arg) => k + arg, {|MA0105:(k, v, arg) => v + factoryArg|}, factoryArg);
            """;
        test.FixedCode = """
            using System.Collections.Concurrent;

            var key = 1;
            var factoryArg = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(key, (k, arg) => k + arg, (k, v, arg) => v + arg, factoryArg);
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
    public Task GetOrAdd_CapturedVariableIsWritten_IsValid()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var counter = 0;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(1, _ => ++counter);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_CapturedVariableIsWrittenInANestedLambda_IsValid()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System;
            using System.Collections.Concurrent;

            var counter = 0;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(1, _ => new Func<int>(() => ++counter)());
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_CapturedVariableIsWrittenByTheUpdateValueFactory_NoCodeFix()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Collections.Concurrent;

            var counter = 0;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(1, {|MA0106:k => counter|}, (k, v) => ++counter);
            """;

        // The update value factory writes 'counter', so it cannot use the 'factoryArgument' parameter
        test.FixedCode = """
            using System.Collections.Concurrent;

            var counter = 0;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(1, {|MA0106:k => counter|}, (k, v) => ++counter);
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

    [Fact]
    public Task GetOrAdd_ExplicitlyTypedLambda_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Concurrent;

            class Test
            {
                object A()
                {
                    var value = 42;
                    var a = new ConcurrentDictionary<string, int>();
                    return a.GetOrAdd("key", {|MA0106:(string key) => value|});
                }
            }
            """;
        test.FixedCode = """
            using System.Collections.Concurrent;

            class Test
            {
                object A()
                {
                    var value = 42;
                    var a = new ConcurrentDictionary<string, int>();
                    return a.GetOrAdd("key", (string key, int arg) => arg, value);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_ExplicitlyTypedLambda_QualifiedType_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Concurrent;

            class Test
            {
                object A()
                {
                    var value = new System.Text.StringBuilder();
                    var a = new ConcurrentDictionary<string, int>();
                    return a.GetOrAdd("key", {|MA0106:(string key) => value.Length|});
                }
            }
            """;
        test.FixedCode = """
            using System.Collections.Concurrent;

            class Test
            {
                object A()
                {
                    var value = new System.Text.StringBuilder();
                    var a = new ConcurrentDictionary<string, int>();
                    return a.GetOrAdd("key", (string key, System.Text.StringBuilder arg) => arg.Length, value);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_ExplicitlyTypedLambda_NullableType_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            #nullable enable
            using System.Collections.Concurrent;

            class Test
            {
                object A(string? value)
                {
                    var a = new ConcurrentDictionary<string, int>();
                    return a.GetOrAdd("key", {|MA0106:(string key) => value?.Length ?? 0|});
                }
            }
            """;
        test.FixedCode = """
            #nullable enable
            using System.Collections.Concurrent;

            class Test
            {
                object A(string? value)
                {
                    var a = new ConcurrentDictionary<string, int>();
                    return a.GetOrAdd("key", (string key, string? arg) => arg?.Length ?? 0, value);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_ExplicitlyTypedLambdas_CodeFix()
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
                    a.AddOrUpdate(key, {|MA0106:(int k) => value|}, {|MA0106:(int k, int v) => value|});
                }
            }
            """;
        test.FixedCode = """
            using System.Collections.Concurrent;

            class Test
            {
                void A()
                {
                    var key = 1;
                    var value = 1;
                    var a = new ConcurrentDictionary<int, int>();
                    a.AddOrUpdate(key, (int k, int arg) => arg, (int k, int v, int arg) => arg, value);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_OnlyTheUpdateValueFactoryIsExplicitlyTyped_CodeFix()
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
                    a.AddOrUpdate(key, k => k, {|MA0106:(int k, int v) => value|});
                }
            }
            """;
        test.FixedCode = """
            using System.Collections.Concurrent;

            class Test
            {
                void A()
                {
                    var key = 1;
                    var value = 1;
                    var a = new ConcurrentDictionary<int, int>();
                    a.AddOrUpdate(key, (k, arg) => k, (int k, int v, int arg) => arg, value);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetOrAdd_LambdaWithDefaultParameterValue_NoCodeFix()
    {
        // No parameter can be added after the parameter with a default value, so the fix is not offered
        var code = """
            using System.Collections.Concurrent;

            class Test
            {
                object A()
                {
                    var value = 42;
                    var a = new ConcurrentDictionary<string, int>();
                    return a.GetOrAdd("key", {|MA0106:(string key = "") => value|});
                }
            }
            """;

        var test = new CodeFixTest();
        test.TestCode = code;
        test.FixedCode = code;

        return test.RunAsync();
    }
}

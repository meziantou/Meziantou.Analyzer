using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.AvoidClosureWhenUsingConcurrentDictionaryAnalyzer,
    Meziantou.Analyzer.Rules.AvoidClosureWhenUsingConcurrentDictionaryFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ConcurrentDictionaryMustPreventClosureWhenAccessingTheKeyAnalyzerTests_MA0105
{
    // This class covers MA0105 only, the way the original test filtered the diagnostics to that rule
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add(RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionaryByUsingFactoryArg);
        return test;
    }

    [Fact]
    public Task GetOrAdd_IsValid()
    {
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Concurrent;

            class Test
            {
                void A(int value)
                {
                    var key = 1;
                    var a = new ConcurrentDictionary<int, int>();
                    a.AddOrUpdate(key, value, (k, oldValue) => value);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_Parameter_IsValid()
    {
        var test = CreateTest();
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
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Concurrent;

            class Test
            {
                void A()
                {
                    var key = 1;
                    var value = 1;
                    var a = new ConcurrentDictionary<int, int>();

                    a.AddOrUpdate(key, addValueFactory: k => k, updateValueFactory: (k, v) => value);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOrUpdate_Variable_netstandard2()
    {
        var test = CreateTest();
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
}

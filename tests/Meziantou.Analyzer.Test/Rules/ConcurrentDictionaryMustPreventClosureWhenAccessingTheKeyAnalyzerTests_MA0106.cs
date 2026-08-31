using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.AvoidClosureWhenUsingConcurrentDictionaryAnalyzer,
    Meziantou.Analyzer.Rules.AvoidClosureWhenUsingConcurrentDictionaryFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ConcurrentDictionaryMustPreventClosureWhenAccessingTheKeyAnalyzerTests_MA0106
{
    // This class covers MA0106 only, the way the original test filtered the diagnostics to that rule
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add(RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionary);
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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

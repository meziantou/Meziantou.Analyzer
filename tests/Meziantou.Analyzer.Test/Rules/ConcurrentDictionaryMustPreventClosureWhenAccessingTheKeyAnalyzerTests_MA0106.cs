using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public class ConcurrentDictionaryMustPreventClosureWhenAccessingTheKeyAnalyzerTests_MA0106
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.Net6_0)
            .WithAnalyzer<AvoidClosureWhenUsingConcurrentDictionaryAnalyzer>(id: "MA0106");
    }

    [Fact]
    public async Task GetOrAdd_IsValid()
    {
        const string SourceCode = """
            using System.Collections.Concurrent;
            
            var key = 1;
            var value = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, (k) => k + 1);
            a.GetOrAdd(key, (_, v) => v, value);
            """;
        await CreateProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task GetOrAdd_NoOverload_IsValid()
    {
        const string SourceCode = """
            using System.Collections.Concurrent;
            
            var key = 1;
            var value = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, (k) => k + 1);
            a.GetOrAdd(key, _ => value);
            """;
        await CreateProjectBuilder()
            .WithTargetFramework(TargetFramework.NetStandard2_0) // No overload
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task GetOrAdd_TArg_IsValid()
    {
        const string SourceCode = """
            using System;
            using System.Collections.Concurrent;
            
            var key = 1;
            var closure = """
            """;
var a = new ConcurrentDictionary<int, Func<string>>();
a.GetOrAdd<Func<string>>(key, (_, v) => v, () => closure);
";
        await CreateProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task GetOrAdd_Key_IsValid()
    {
        const string SourceCode = """
            using System;
            using System.Collections.Concurrent;
            
            var key = 1;
            var closure = """
            """;
var a = new ConcurrentDictionary<Func<string>, int>();
a.GetOrAdd(() => closure, _ => 0);
";
        await CreateProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task GetOrAdd_Closure()
    {
        const string SourceCode = """
            using System.Collections.Concurrent;
            
            var key = 1;
            var value = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, [|_ => value|]);
            """;
        await CreateProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task GetOrAdd_ClosureWithLambdaParameter()
    {
        const string SourceCode = """
            using System.Collections.Concurrent;
            
            var key = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, k => new System.Func<int>(() => k)());
            """;
        await CreateProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task GetOrAdd_NoClosure()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }
}

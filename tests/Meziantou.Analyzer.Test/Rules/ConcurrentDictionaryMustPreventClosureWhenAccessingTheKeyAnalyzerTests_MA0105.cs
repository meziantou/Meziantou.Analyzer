using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public class ConcurrentDictionaryMustPreventClosureWhenAccessingTheKeyAnalyzerTests_MA0105
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.Net6_0)
            .WithAnalyzer<AvoidClosureWhenUsingConcurrentDictionaryAnalyzer>(id: "MA0105");
    }

    [Fact]
    public async Task GetOrAdd_IsValid()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task AddOrUpdate_IsValid()
    {
        const string SourceCode = """
            using System.Collections.Concurrent;
            
            var key = 1;
            var value = 1;
            var factoryArg = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.AddOrUpdate(key, value, (k, v) => k + v);
            a.AddOrUpdate(key, (k) => k, (k, v) => k + v + 1);
            a.AddOrUpdate(key, (k, arg) => k + arg, (k, v, arg) => k + v + arg, factoryArg);
            """;
        await CreateProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task GetOrAdd()
    {
        const string SourceCode = """
            using System.Collections.Concurrent;
            
            var key = 1;
            var value = 1;
            var a = new ConcurrentDictionary<int, int>();
            a.GetOrAdd(key, [|k => key|]);
            """;
        await CreateProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task GetOrAdd_StringInterpolation()
    {
        const string SourceCode = """
            using System.Collections.Concurrent;
            
            var key = 1;
            var value = 1;
            var dict = new ConcurrentDictionary<int, string>();
            dict.GetOrAdd(key, [|k => $""{key}""|]);
            """;
        await CreateProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task AddOrUpdate_Parameter()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task AddOrUpdate_Parameter_IsValid()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task AddOrUpdate_Variable_IsValid()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/569")]
    public async Task AddOrUpdate_Variable_netstandard2()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
            .WithTargetFramework(TargetFramework.NetStandard2_0)
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }
}

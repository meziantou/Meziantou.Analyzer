using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffixAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffixAnalyzer>()
            .WithTargetFramework(TargetFramework.Net8_0)
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview);
    }

    [Fact]
    public async Task AsyncMethodWithSuffix()
    {
        const string SourceCode = """
            class TypeName
            {
                System.Threading.Tasks.Task TestAsync() => throw null;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AsyncMethodWithoutSuffix()
    {
        const string SourceCode = """
            class TypeName
            {
                System.Threading.Tasks.Task {|MA0137:Test|}() => throw null;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
    [Fact]
    public async Task VoidMethodWithSuffix()
    {
        const string SourceCode = """
            class TypeName
            {
                void {|MA0138:TestAsync|}() => throw null;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task VoidMethodWithoutSuffix()
    {
        const string SourceCode = """
            class TypeName
            {
                void Test() => throw null;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task VoidLocalFunctionWithSuffix()
    {
        const string SourceCode = """
            class TypeName
            {
                void Test()
                {
                    void {|MA0138:FooAsync|}() => throw null;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task VoidLocalFunctionWithoutSuffix()
    {
        const string SourceCode = """
            class TypeName
            {
                void Test()
                {
                    void Foo() => throw null;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AwaitableLocalFunctionWithoutSuffix()
    {
        const string SourceCode = """
            class TypeName
            {
                void Test()
                {
                    _ = Foo();
                    System.Threading.Tasks.Task {|MA0137:Foo|}() => throw null;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AwaitableLocalFunctionWithSuffix()
    {
        const string SourceCode = """
            class TypeName
            {
                void Test()
                {
                    System.Threading.Tasks.Task FooAsync() => throw null;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task TopLevelStatement()
    {
        const string SourceCode = """
            await System.Threading.Tasks.Task.Yield();
            """;
        await CreateProjectBuilder()
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task EntryPoint()
    {
        const string SourceCode = """
            static class Program
            {
                static async System.Threading.Tasks.Task Main()
                {
                }
            }
            """;
        await CreateProjectBuilder()
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IAsyncEnumerableWithoutSuffix()
    {
        const string SourceCode = """
            class TypeName
            {
                System.Collections.Generic.IAsyncEnumerable<int> {|MA0156:Foo|}() => throw null;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Method returning IAsyncEnumerable<T> must use the 'Async' suffix")
              .ValidateAsync();
    }

    [Fact]
    public async Task IAsyncEnumerableWithSuffix()
    {
        const string SourceCode = """
            class TypeName
            {
                System.Collections.Generic.IAsyncEnumerable<int> {|MA0157:FooAsync|}() => throw null;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Method not returning IAsyncEnumerable<T> must not use the 'Async' suffix")
              .ValidateAsync();
    }
}

using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffixAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
        => new ProjectBuilder()
            .WithAnalyzer<MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffixAnalyzer>()
            .WithTargetFramework(TargetFramework.Net8_0)
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview);

    [Fact]
    public Task AsyncMethodWithSuffix()
        => CreateProjectBuilder()
              .WithSourceCode("""
                    class TypeName
                    {
                        System.Threading.Tasks.Task TestAsync() => throw null;
                    }
                """)
              .ValidateAsync();

    [Fact]
    public Task AsyncMethodWithoutSuffix()
        => CreateProjectBuilder()
              .WithSourceCode("""
                class TypeName
                {
                    System.Threading.Tasks.Task {|MA0137:Test|}() => throw null;
                }
                """)
              .ValidateAsync();
    [Fact]
    public Task VoidMethodWithSuffix()
        => CreateProjectBuilder()
              .WithSourceCode("""
                class TypeName
                {
                    void {|MA0138:TestAsync|}() => throw null;
                }
                """)
              .ValidateAsync();

    [Fact]
    public Task VoidMethodWithoutSuffix()
        => CreateProjectBuilder()
              .WithSourceCode("""
                class TypeName
                {
                    void Test() => throw null;
                }
                """)
              .ValidateAsync();

    [Fact]
    public Task VoidLocalFunctionWithSuffix()
        => CreateProjectBuilder()
              .WithSourceCode("""
                class TypeName
                {
                    void Test()
                    {
                        void {|MA0138:FooAsync|}() => throw null;
                    }
                }
                """)
              .ValidateAsync();

    [Fact]
    public async Task VoidLocalFunctionWithoutSuffix()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                class TypeName
                {
                    void Test()
                    {
                        void Foo() => throw null;
                    }
                }
                """)
              .ValidateAsync();

    [Fact]
    public Task AwaitableLocalFunctionWithoutSuffix()
        => CreateProjectBuilder()
              .WithSourceCode("""
                class TypeName
                {
                    void Test()
                    {
                        _ = Foo();
                        System.Threading.Tasks.Task {|MA0137:Foo|}() => throw null;
                    }
                }
                """)
              .ValidateAsync();

    [Fact]
    public Task AwaitableLocalFunctionWithSuffix()
        => CreateProjectBuilder()
              .WithSourceCode("""
                class TypeName
                {
                    void Test()
                    {
                        System.Threading.Tasks.Task FooAsync() => throw null;
                    }
                }
                """)
              .ValidateAsync();

    [Fact]
    public Task TopLevelStatement()
        => CreateProjectBuilder()
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
              .WithSourceCode("""
                await System.Threading.Tasks.Task.Yield();
                """)
              .ValidateAsync();

    [Fact]
    public Task EntryPoint()
        => CreateProjectBuilder()
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
              .WithSourceCode("""
                 static class Program
                 {
                     static async System.Threading.Tasks.Task Main()
                     {
                     }
                 }
                 """)
              .ValidateAsync();

    [Fact]
    public Task IAsyncEnumerableWithoutSuffix()
        => CreateProjectBuilder()
              .WithSourceCode("""
                 class TypeName
                 {
                     System.Collections.Generic.IAsyncEnumerable<int> {|MA0156:Foo|}() => throw null;
                 }
                 """)
              .ShouldReportDiagnosticWithMessage("Method returning IAsyncEnumerable<T> must use the 'Async' suffix")
              .ValidateAsync();

    [Fact]
    public Task IAsyncEnumerableWithSuffix()
        => CreateProjectBuilder()
              .WithSourceCode("""
                 class TypeName
                 {
                     System.Collections.Generic.IAsyncEnumerable<int> {|MA0157:FooAsync|}() => throw null;
                 }
                 """)
              .ShouldReportDiagnosticWithMessage("Method not returning IAsyncEnumerable<T> must not use the 'Async' suffix")
              .ValidateAsync();

    [Fact]
    public Task IgnoreTestMethods()
        => CreateProjectBuilder()
              .WithSourceCode("""
                 class TypeName
                 {
                     [Xunit.Fact]
                     System.Threading.Tasks.Task Foo() => throw null;
                 }
                 """)
              .AddXUnitApi()
              .ValidateAsync();
}

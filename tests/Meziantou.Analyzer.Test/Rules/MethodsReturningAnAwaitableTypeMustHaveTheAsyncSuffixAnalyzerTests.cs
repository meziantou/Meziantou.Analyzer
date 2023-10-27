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
                System.Threading.Tasks.Task [|Test|]() => throw null;
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
                void [|TestAsync|]() => throw null;
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
                    void [|FooAsync|]() => throw null;
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
                    System.Threading.Tasks.Task [|Foo|]() => throw null;
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
}

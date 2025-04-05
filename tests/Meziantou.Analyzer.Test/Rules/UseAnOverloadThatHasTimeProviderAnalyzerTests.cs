using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseAnOverloadThatHasTimeProviderAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.Net9_0)
            .WithAnalyzer<UseAnOverloadThatHasTimeProviderAnalyzer>()
            .WithCodeFixProvider<UseAnOverloadThatHasTimeProviderFixer>();
    }

    [Fact]
    public async Task NoReport_ConsoleWriteLine()
    {
        const string SourceCode = """
            System.Console.WriteLine("test");
            """;
        await CreateProjectBuilder()
              .WithOutputKind(OutputKind.ConsoleApplication)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }


    [Fact]
    public async Task NotAvailable()
    {
        const string SourceCode = """
            class Test
            {
                void A()
                {
                    {|MA0167:System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero)|};
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoReport_WrongOverload()
    {
        const string SourceCode = """
            class Test
            {
                public void A()
                {
                    B();
                }

                void B() { }
                void B(int a) { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task WhenAvailable_Parameter()
    {
        const string SourceCode = """
            class Test
            {
                void A(System.TimeProvider foo)
                {
                    [|System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero)|];
                }
            }
            """;

        const string Fix = """
            class Test
            {
                void A(System.TimeProvider foo)
                {
                    System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero, foo);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task WhenAvailable_NestedProp()
    {
        const string SourceCode = """
            class Test
            {
                void A(Sample foo)
                {
                    [|System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero)|];
                }

                class Sample { public System.TimeProvider A {get;} }
            }
            """;

        const string Fix = """
            class Test
            {
                void A(Sample foo)
                {
                    System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero, foo.A);
                }
            
                class Sample { public System.TimeProvider A {get;} }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }
}

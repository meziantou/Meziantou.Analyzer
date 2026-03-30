using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseLazyInitializerEnsureInitializeAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseLazyInitializerEnsureInitializeAnalyzer>()
            .WithCodeFixProvider<UseLazyInitializerEnsureInitializeFixer>()
            .WithTargetFramework(TargetFramework.NetLatest)
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication);
    }

    [Fact]
    public async Task NewObject_Null()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                object a = default;
                [|System.Threading.Interlocked.CompareExchange(ref a, new object(), null)|];
                """)
              .ShouldFixCodeWith("""
                object a = default;
                System.Threading.LazyInitializer.EnsureInitialized(ref a, () => new object());
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NewCustomClass_Null()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                Sample a = default;
                [|System.Threading.Interlocked.CompareExchange(ref a, new Sample(), null)|];
                class Sample { };
                """)
              .ShouldFixCodeWith("""
                Sample a = default;
                System.Threading.LazyInitializer.EnsureInitialized(ref a, () => new Sample());
                class Sample { };
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NewCustomClass_Object_Null()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                object? a = default;
                [|System.Threading.Interlocked.CompareExchange(ref a, new Sample(), null)|];
                class Sample { };
                """)
              .ShouldFixCodeWith("""
                object? a = default;
                System.Threading.LazyInitializer.EnsureInitialized(ref a, () => new Sample());
                class Sample { };
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NewCustomClass_Default()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                Sample a = default;
                [|System.Threading.Interlocked.CompareExchange(ref a, new Sample(), default)|];
                class Sample { };
                """)
              .ShouldFixCodeWith("""
                Sample a = default;
                System.Threading.LazyInitializer.EnsureInitialized(ref a, () => new Sample());
                class Sample { };
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NewCustomStruct()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                Sample a = default;
                System.Threading.Interlocked.CompareExchange(ref a, new Sample(), default);
                struct Sample { };
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NewInt32_Zero()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                int a = default;
                System.Threading.Interlocked.CompareExchange(ref a, 0, 0);
                """)
              .ValidateAsync();
    }
}
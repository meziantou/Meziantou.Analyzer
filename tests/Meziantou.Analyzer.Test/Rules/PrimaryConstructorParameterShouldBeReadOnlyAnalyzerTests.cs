#if CSHARP12_OR_GREATER
using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class PrimaryConstructorParameterShouldBeReadOnlyAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<PrimaryConstructorParameterShouldBeReadOnlyAnalyzer>()
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12);
    }

    [Fact]
    public async Task AssignClassicCtorParameter()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Test
                {
                    Test(int p) => p++;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AssignClassicParameter()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Test
                {
                    void A(int p) => p++;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AssignUsingIncrementOperator()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Test(int p)
                {
                    int A() => [|p|]++;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AssignUsingDecrementOperator()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Test(int p)
                {
                    int A() => [|p|]--;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AssignUsingInfixDecrementOperator()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Test(int p)
                {
                    int A() => --[|p|];
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task DeconstructionAssignment()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Test(int p)
                {
                    void A()
                    {
                        ([|p|], _) = (1, 0);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Deconstruction_Deep_Assignment()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Test(int p)
                {
                    void A()
                    {
                        (var a, ([|p|], _)) = (0, (1, 2));
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CoalesceAssignment()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Test(string p)
                {
                    void A()
                    {
                        [|p|] ??= "";
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CompoundAssignment()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Test(string p)
                {
                    void A()
                    {
                        [|p|] += "";
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AssignVariable()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Test(string p)
                {
                    void A()
                    {
                        var a = p;
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Test(string p)
                {
                    void A(string value)
                    {
                        A(p);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task EditUsingRefVariable()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Test(string p)
                {
                    void A()
                    {
                        ref var a = ref [|p|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task EditUsingRefParameter()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Test(string p)
                {
                    void A(ref string a)
                    {
                        A(ref [|p|]);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task EditUsingInParameter()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Test(string p)
                {
                    void A(in string a)
                    {
                        A(in p);
                    }
                }
                """)
              .ValidateAsync();
    }
}
#endif

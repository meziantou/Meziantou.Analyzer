using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class IfElseBranchesAreIdenticalAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder() =>
        new ProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithAnalyzer<IfElseBranchesAreIdenticalAnalyzer>();

    [Fact]
    public Task IfElse_SameCode() => CreateProjectBuilder()
        .WithSourceCode("""
            [||]if(true)
                _ = "";
            else
                _ = "";
            """)
        .ValidateAsync();

    [Fact]
    public Task IfElse_SameCode_WithComments() => CreateProjectBuilder()
        .WithSourceCode("""
            [||]if(true)
            {
                _ = "";
            }
            else
            {
                // test
                _ = "";
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task IfElse_DifferentBranches() => CreateProjectBuilder()
        .WithSourceCode("""
            if(true)
            {
                _ = "";
            }
            else
            {
                // test
                _ = 10;
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task If_WithoutElse() => CreateProjectBuilder()
        .WithSourceCode("""
            if(true)
            {
                _ = "";
            }
        """)
        .ValidateAsync();

    [Fact]
    public Task Ternary_SameCode() => CreateProjectBuilder()
        .WithSourceCode("""_ = [||]true ? 0 : 0;""")
        .ValidateAsync();

    [Fact]
    public Task Ternary_Different() => CreateProjectBuilder()
        .WithSourceCode("""_ = true ? 0 : 1;""")
        .ValidateAsync();

    [Fact]
    public Task IfElse_WithLocalFunction() => CreateProjectBuilder()
        .WithSourceCode("""
            [||]if(true)
            {
                _ = "";
                void A() => A();
            }
            else
            {
                _ = "";
                void A() => A();
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task IfElse_WithLocalFunction_Different() => CreateProjectBuilder()
        .WithSourceCode("""
            if(true)
            {
                _ = "";
                void A() => A();
            }
            else
            {
                _ = "";
                void A() => throw null;
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task IfWithoutElse_ButSingleStatement() => CreateProjectBuilder()
        .WithSourceCode("""
            [||]if(true)
                return 0;
            return 0;
            """)
        .ValidateAsync();

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_NotGlobalStatement() => CreateProjectBuilder()
        .WithSourceCode("""
            A();
            int A()
            {
                [||]if(true)
                    return 0;
                return 0;
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_DeadCode() => CreateProjectBuilder()
        .WithSourceCode("""
            A();
            int A()
            {
                [||]if(true)
                    return 0;
                return 0;
                System.Console.WriteLine();
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_Break() => CreateProjectBuilder()
        .WithSourceCode("""
            A();
            void A()
            {
                while (true){
                    [||]if (true)
                        break;
                    break;
                }
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_Continue() => CreateProjectBuilder()
        .WithSourceCode("""
            A();
            void A()
            {
                while (true){
                    [||]if (true)
                        continue;
                    continue;
                }
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_goto() => CreateProjectBuilder()
        .WithSourceCode("""
            A();
            void A()
            {
                sample:
                while (true){
                    [||]if (true)
                        goto sample;
                    goto sample;
                }
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_DifferentCode1() => CreateProjectBuilder()
        .WithSourceCode("""
            A();
            int A()
            {
                if(true)
                    return 0;

                System.Console.WriteLine();
                return 0;
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_DifferentCode2() => CreateProjectBuilder()
        .WithSourceCode("""
            A();
            int A()
            {
                if(true)
                    return 0;
                return 1;
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_SameCodeButNotReturn() => CreateProjectBuilder()
        .WithSourceCode("""
            A();
            int A()
            {
                if(true)
                {
                    System.Console.WriteLine();
                }
                System.Console.WriteLine();
                return 0;
            }
            """)
        .ValidateAsync();
}

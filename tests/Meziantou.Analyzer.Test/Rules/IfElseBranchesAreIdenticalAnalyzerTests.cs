using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.IfElseBranchesAreIdenticalAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class IfElseBranchesAreIdenticalAnalyzerTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task IfElse_SameCode()
    {
        var test = CreateTest();
        test.TestCode = """
            [|if(true)
                _ = "";
            else
                _ = "";|]
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfElse_SameCode_WithComments()
    {
        var test = CreateTest();
        test.TestCode = """
            [|if(true)
            {
                _ = "";
            }
            else
            {
                // test
                _ = "";
            }|]
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfElse_DifferentBranches()
    {
        var test = CreateTest();
        test.TestCode = """
            if(true)
            {
                _ = "";
            }
            else
            {
                // test
                _ = 10;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task If_WithoutElse()
    {
        var test = CreateTest();
        test.TestCode = """
            if(true)
            {
                _ = "";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Ternary_SameCode()
    {
        var test = CreateTest();
        test.TestCode = """_ = [|true ? 0 : 0|];""";

        return test.RunAsync();
    }

    [Fact]
    public Task Ternary_Different()
    {
        var test = CreateTest();
        test.TestCode = """_ = true ? 0 : 1;""";

        return test.RunAsync();
    }

    [Fact]
    public Task IfElse_WithLocalFunction()
    {
        var test = CreateTest();
        test.TestCode = """
            [|if(true)
            {
                _ = "";
                void A() => A();
            }
            else
            {
                _ = "";
                void A() => A();
            }|]
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfElse_WithLocalFunction_Different()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfWithoutElse_ButSingleStatement()
    {
        var test = CreateTest();
        test.TestCode = """
            [|if(true)
                return 0;|]
            return 0;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_NotGlobalStatement()
    {
        var test = CreateTest();
        test.TestCode = """
            A();
            int A()
            {
                [|if(true)
                    return 0;|]
                return 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_DeadCode()
    {
        var test = CreateTest();
        test.TestCode = """
            A();
            int A()
            {
                [|if(true)
                    return 0;|]
                return 0;
                System.Console.WriteLine();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_Break()
    {
        var test = CreateTest();
        test.TestCode = """
            A();
            void A()
            {
                while (true){
                    [|if (true)
                        break;|]
                    break;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_Continue()
    {
        var test = CreateTest();
        test.TestCode = """
            A();
            void A()
            {
                while (true){
                    [|if (true)
                        continue;|]
                    continue;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_goto()
    {
        var test = CreateTest();
        test.TestCode = """
            A();
            void A()
            {
                sample:
                while (true){
                    [|if (true)
                        goto sample;|]
                    goto sample;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_DifferentCode1()
    {
        var test = CreateTest();
        test.TestCode = """
            A();
            int A()
            {
                if(true)
                    return 0;

                System.Console.WriteLine();
                return 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_DifferentCode2()
    {
        var test = CreateTest();
        test.TestCode = """
            A();
            int A()
            {
                if(true)
                    return 0;
                return 1;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfWithoutElse_ButSingleStatement_SameCodeButNotReturn()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }
}

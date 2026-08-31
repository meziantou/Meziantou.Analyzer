using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.RemoveEmptyStatementAnalyzer,
    Meziantou.Analyzer.Rules.RemoveEmptyStatementFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RemoveEmptyStatementAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task EmptyStatement()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
                    {|MA0037:;|}
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                public void A()
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyInLoopStatement()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
                    while(true)
                    {
                        {|MA0037:;|}
                    }
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                public void A()
                {
                    while(true)
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WhileStatement()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
                    while(true)
                        {|MA0037:;|}
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                public void A()
                {
                    while(true)
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ForStatement()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
                    for(;;)
                        {|MA0037:;|}
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                public void A()
                {
                    for(;;)
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ForEachStatement()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
                    foreach(var a in new []{0})
                        {|MA0037:;|}
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                public void A()
                {
                    foreach(var a in new []{0})
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyStatementInALabel()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
            test:
                    ;
                }
            }
            """;

        return test.RunAsync();
    }
}

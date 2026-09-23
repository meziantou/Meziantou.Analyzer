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

    [Theory]
    [InlineData("if (b)")]
    [InlineData("if (b) { } else")]
    [InlineData("while (b)")]
    [InlineData("lock (o)")]
    [InlineData("using (d)")]
    [InlineData("foreach (var (x, y) in items)")]
    public Task EmbeddedStatement(string statement)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class Test
            {
                public void A(bool b, object o, System.IDisposable d, (int, int)[] items)
                {
                    {{statement}}
                        {|MA0037:;|}
                }
            }
            """;
        test.FixedCode = $$"""
            class Test
            {
                public void A(bool b, object o, System.IDisposable d, (int, int)[] items)
                {
                    {{statement}}
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmbeddedStatement_SameLine()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A(bool b)
                {
                    if (b) {|MA0037:;|}
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                public void A(bool b)
                {
                    if (b)
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DoStatement()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A(bool b)
                {
                    do
                        {|MA0037:;|}
                    while (b);
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                public void A(bool b)
                {
                    do
                    {
                    }
                    while (b);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FixedStatement()
    {
        var test = CreateTest();
        test.SolutionTransforms.Add(static (solution, projectId) =>
            solution.WithProjectCompilationOptions(projectId, ((CSharpCompilationOptions)solution.GetProject(projectId)!.CompilationOptions!).WithAllowUnsafe(true)));
        test.TestCode = """
            unsafe class Test
            {
                public void A(int[] array)
                {
                    fixed (int* p = array)
                        {|MA0037:;|}
                }
            }
            """;
        test.FixedCode = """
            unsafe class Test
            {
                public void A(int[] array)
                {
                    fixed (int* p = array)
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyStatementInSwitchSection()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A(int value)
                {
                    switch (value)
                    {
                        case 0:
                            {|MA0037:;|}
                            break;
                    }
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                public void A(int value)
                {
                    switch (value)
                    {
                        case 0:
                            break;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyStatementInTopLevelStatements()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            System.Console.WriteLine();
            {|MA0037:;|}
            """;
        test.FixedCode = """
            System.Console.WriteLine();

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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.MethodShouldNotBeTooLongAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MethodShouldNotBeTooLongAnalyzerTests
{
    private static AnalyzerTest CreateTest(params (string Key, string Value)[] configuration)
    {
        var test = new AnalyzerTest();
        test.TestState.SetConfiguration(configuration);
        return test;
    }

    [Fact]
    public Task TooLongMethod_Statements()
    {
        var test = CreateTest(("MA0051.maximum_statements_per_method", "2"));
        test.TestCode = """
            public class Test
            {
                void {|MA0051:Method|}()
                {
                    var a = 0;var b = 0;
                    void A(){var c = 0;}
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidMethod_Statements()
    {
        var test = CreateTest(("MA0051.maximum_statements_per_method", "3"));
        test.TestCode = """
            public class Test
            {
                void Method()
                {
                    var a = 0;var b = 0;var c = 0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TooLongMethod_Lines()
    {
        var test = CreateTest(("MA0051.maximum_lines_per_method", "2"));
        test.TestCode = """
            public class Test
            {
                void {|MA0051:Method|}()
                {
                    var a = 0;var d = 0;
                    var b = 0;var e = 0;
                    var c = 0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidMethod_Lines()
    {
        var test = CreateTest(("MA0051.maximum_lines_per_method", "4"));
        test.TestCode = """
            public class Test
            {
                void Method()
                {
                    var a = 0;var d = 0;
                    var b = 0;var e = 0;
                    var c = 0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TooLong_SkipLocalFunction_False()
    {
        var test = CreateTest(
            ("MA0051.maximum_lines_per_method", "5"),
            ("MA0051.skip_local_functions", "false"));
        test.TestCode = """
            public class Test
            {
                void {|MA0051:Method|}()
                {
                    var a = 0;
                    var b = 0;
                    var c = 0;

                    void A()
                    {
                        void B() { }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidMethod_SkipLocalFunction_True()
    {
        var test = CreateTest(
            ("MA0051.maximum_lines_per_method", "5"),
            ("MA0051.skip_local_functions", "true"));
        test.TestCode = """
            public class Test
            {
                void Method()
                {
                    var a = 0;
                    var b = 0;
                    var c = 0;

                    void A()
                    {
                        void B() { }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidMethod_Statements_SkipLocalFunction()
    {
        var test = CreateTest(
            ("MA0051.maximum_lines_per_method", "-1"),
            ("MA0051.maximum_statements_per_method", "2"),
            ("MA0051.skip_local_functions", "true"));
        test.TestCode = """
            public class Test
            {
                void Method()
                {
                    var a = 0;
                    var b = 0;

                    void A()
                    {
                        _ = "";

                        void B() { }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public void CountStatement_ForLoop()
    {
        const string SourceCode = """
            for (int a = 0; i < 0; i++)
            {
                throw null;
            }
            """;

        var count = CountStatements(SourceCode);
        Assert.Equal(2, count);
    }

    [Fact]
    public void CountStatement_LocalFunction()
    {
        const string SourceCode = """
            throw null;

            void B()
            {
                throw null;
            }
            """;

        var count = CountStatements(SourceCode);
        Assert.Equal(2, count);
    }

    [Fact]
    public void CountStatement_If()
    {
        const string SourceCode = """
            if (true)
            {
                throw null;
            }
            """;

        var count = CountStatements(SourceCode);
        Assert.Equal(2, count);
    }

    private static int CountStatements(string code)
    {
        var tree = CSharpSyntaxTree.ParseText("void A(){ " + code + " }");

        var root = tree.GetRoot().DescendantNodes().OfType<BlockSyntax>().First();
        return MethodShouldNotBeTooLongAnalyzer.CountStatements(default, root);
    }
}

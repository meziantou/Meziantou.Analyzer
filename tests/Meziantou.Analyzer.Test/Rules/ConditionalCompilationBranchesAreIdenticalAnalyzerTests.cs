using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.ConditionalCompilationBranchesAreIdenticalAnalyzer,
    Meziantou.Analyzer.Rules.ConditionalCompilationBranchesAreIdenticalFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ConditionalCompilationBranchesAreIdenticalAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task IfElif_SameCode()
    {
        var test = CreateTest();
        test.TestCode = """
            #if A
            _ = 0;
            {|MA0202:#elif B|}
            _ = 0;
            #else
            _ = 1;
            #endif
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfElse_SameCode()
    {
        var test = CreateTest();
        test.TestCode = """
            #if A
            _ = 0;
            {|MA0202:#else|}
            _ = 0;
            #endif
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAdjacentDuplicateBranch()
    {
        var test = CreateTest();
        test.TestCode = """
            #if A
            _ = 0;
            #elif B
            _ = 1;
            {|MA0202:#else|}
            _ = 0;
            #endif
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SameCodeWithDifferentComments()
    {
        var test = CreateTest();
        test.TestCode = """
            #if A
            _ = 0;
            {|MA0202:#elif B|}
            // comment
            _ = 0;
            #else
            _ = 1;
            #endif
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DifferentXmlCommentsOnly()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
            #if A
                /// <summary>net8</summary>
            #else
                /// <summary>net9</summary>
            #endif
                void M() { }
            }

            static class Program { static void Main() { } }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SameXmlCommentsOnly()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
            #if A
                /// <summary>text</summary>
            {|MA0202:#else|}
                /// <summary>text</summary>
            #endif
                void M() { }
            }

            static class Program { static void Main() { } }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DifferentBranches()
    {
        var test = CreateTest();
        test.TestCode = """
            #if A
            _ = 0;
            #elif B
            _ = 1;
            #else
            _ = 2;
            #endif
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfElse_SameCode_PartialExpression()
    {
        var test = CreateTest();
        test.TestCode = """
            _ =
            #if A
             1;
            {|MA0202:#else|}
             1;
            #endif
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IfElse_SameCode_PartialTypeDeclaration()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ISample { }
            interface ISpanFormattable { }

            #if A
             public
            #else
             internal
            #endif
            class Sample : ISample
            #if NET10_0
            , ISpanFormattable
            {|MA0202:#else|}
            , ISpanFormattable
            #endif
            { }

            static class Program { static void Main() { } }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Fix_IfElif_SameCode_MergesConditions()
    {
        var test = CreateTest();
        test.TestCode = """
            #if A
            _ = 0;
            {|MA0202:#elif B|}
            _ = 0;
            #else
            _ = 1;
            #endif
            """;
        test.FixedCode = """
            #if (A) || (B)
            _ = 0;
            #else
            _ = 1;
            #endif
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Fix_IfElse_SameCode_RemovesPreprocessor()
    {
        var test = CreateTest();
        test.TestCode = """
            #if A
            _ = 0;
            {|MA0202:#else|}
            _ = 0;
            #endif
            """;
        test.FixedCode = "_ = 0;\n";

        return test.RunAsync();
    }

    [Fact]
    public Task Fix_IfElse_SameCode_PartialExpression_RemovesPreprocessor()
    {
        var test = CreateTest();
        test.TestCode = """
            _ =
            #if A
             1;
            {|MA0202:#else|}
             1;
            #endif
            """;
        test.FixedCode = "_ =\n 1;\n";

        return test.RunAsync();
    }

    [Fact]
    public Task Fix_IfElse_SameCode_PartialTypeDeclaration_RemovesPreprocessor()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ISample { }
            interface ISpanFormattable { }

            #if A
             public
            #else
             internal
            #endif
            class Sample : ISample
            #if NET10_0
            , ISpanFormattable
            {|MA0202:#else|}
            , ISpanFormattable
            #endif
            { }

            static class Program { static void Main() { } }
            """;
        test.FixedCode = """
            interface ISample { }
            interface ISpanFormattable { }

            #if A
             public
            #else
             internal
            #endif
            class Sample : ISample
            , ISpanFormattable
            { }

            static class Program { static void Main() { } }
            """;

        return test.RunAsync();
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.FixToDoAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class FixToDoAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    private static DiagnosticResult ExpectedToDo(int markupKey, string todo) =>
        new DiagnosticResult(RuleIdentifiers.FixToDo, DiagnosticSeverity.Warning)
            .WithLocation(markupKey)
            .WithMessage($"TODO {todo}");

    [Theory]
    [InlineData("//")]
    [InlineData("//test")]
    [InlineData("//TOD")]
    [InlineData("//TODOA")]
    [InlineData("//TODO-A")]
    [InlineData("// (TODO)")]
    public Task SingleLineCommentWithoutTodo(string comment)
    {
        var test = CreateTest();
        test.TestCode = comment;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("//{|#0:TODO|}", "")]
    [InlineData("// {|#0:todo|}", "")]
    [InlineData("// {|#0:ToDo|}", "")]
    [InlineData("// {|#0:TODo|}", "")]
    [InlineData("// {|#0:TODo?|}", "")]
    [InlineData("// {|#0:TODo!|}", "")]
    [InlineData("//{|#0:TODO test|}", "test")]
    [InlineData("// {|#0:TODO test|}", "test")]
    [InlineData("  // {|#0:TODO test|}", "test")]
    [InlineData("  // {|#0:TODO: test|}", "test")]
    public Task SingleLineComment(string comment, string todo)
    {
        var test = CreateTest();
        test.TestCode = comment;
        test.ExpectedDiagnostics.Add(ExpectedToDo(markupKey: 0, todo));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("/*{|#0:TODO|}*/", "")]
    [InlineData("/* {|#0:TODO|}*/", "")]
    [InlineData("/*{|#0:TODO test|}*/", "test")]
    [InlineData("/* {|#0:TODO test|}*/", "test")]
    [InlineData("  /* {|#0:TODO test|}*/", "test")]
    [InlineData("/*\n* {|#0:TODO test|}\r\n*/", "test")]
    public Task MultiLinesComment(string comment, string todo)
    {
        var test = CreateTest();
        test.TestCode = comment;
        test.ExpectedDiagnostics.Add(ExpectedToDo(markupKey: 0, todo));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("/*")]
    [InlineData("/*a")]
    [InlineData("/*/")]
    [InlineData("/*ab")]
    [InlineData("/*test")]
    public Task UnterminatedMultiLinesCommentWithoutTodo(string comment)
    {
        var test = CreateTest();
        // The unterminated comment does not compile, so the compiler diagnostics cannot be verified
        test.CompilerDiagnostics = CompilerDiagnostics.None;
        test.TestCode = comment;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("/*{|#0:TODO|}", "")]
    [InlineData("/* {|#0:TODO|}", "")]
    [InlineData("/*{|#0:TODO test|}", "test")]
    [InlineData("/* {|#0:TODO test|}", "test")]
    [InlineData("/*\n* {|#0:TODO test|}", "test")]
    public Task UnterminatedMultiLinesComment(string comment, string todo)
    {
        var test = CreateTest();
        // The unterminated comment does not compile, so the compiler diagnostics cannot be verified
        test.CompilerDiagnostics = CompilerDiagnostics.None;
        test.TestCode = comment;
        test.ExpectedDiagnostics.Add(ExpectedToDo(markupKey: 0, todo));

        return test.RunAsync();
    }

    [Fact]
    public Task MultiTodoComment()
    {
        var test = CreateTest();
        test.TestCode = """
            /*
             * {|#0:TODO a|}
             * {|#1:TODO: b|}
             */
            """;
        test.ExpectedDiagnostics.Add(ExpectedToDo(markupKey: 0, "a"));
        test.ExpectedDiagnostics.Add(ExpectedToDo(markupKey: 1, "b"));

        return test.RunAsync();
    }
}

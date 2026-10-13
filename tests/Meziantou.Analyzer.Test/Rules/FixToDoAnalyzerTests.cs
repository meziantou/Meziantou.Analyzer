using Meziantou.Analyzer.Internals;
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

    // The text is read by chunks, and the last 3 characters of a chunk are read again by the next one.
    // The offset is relative to the end of the chunk, so "TODO" spans two chunks when the offset is -3, -2 or -1.
    [Theory]
    [InlineData(1, -5)]
    [InlineData(1, -4)]
    [InlineData(1, -3)]
    [InlineData(1, -2)]
    [InlineData(1, -1)]
    [InlineData(1, 0)]
    [InlineData(2, -4)]
    [InlineData(2, -3)]
    [InlineData(2, -2)]
    [InlineData(2, -1)]
    [InlineData(2, 0)]
    public Task TodoAtTheEndOfAReadBuffer(int bufferCount, int offset)
    {
        const string CommentStart = "//";
        var endOfBuffer = (bufferCount * SourceTextSearcher.ReadBufferSize) - ((bufferCount - 1) * 3);
        var todoPosition = endOfBuffer + offset;

        var test = CreateTest();
        test.TestCode = new string(' ', todoPosition - CommentStart.Length) + CommentStart + "{|#0:todo test|}";
        test.ExpectedDiagnostics.Add(ExpectedToDo(markupKey: 0, "test"));

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

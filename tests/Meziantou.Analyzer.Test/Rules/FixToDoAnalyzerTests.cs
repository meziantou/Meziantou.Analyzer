using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;


namespace Meziantou.Analyzer.Test.Rules;

public sealed class FixToDoAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<FixToDoAnalyzer>();
    }

    [Theory]
    [InlineData("//TODOA")]
    [InlineData("// (TODO)")]
    public async Task SingleLineCommentWithoutTodo(string comment)
    {
        await CreateProjectBuilder()
              .WithSourceCode(comment)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("//[||]TODO", "")]
    [InlineData("// [||]TODO", "")]
    [InlineData("//[||]TODO test", "test")]
    [InlineData("// [||]TODO test", "test")]
    [InlineData("  // [||]TODO test", "test")]
    public async Task SingleLineComment(string comment, string todo)
    {
        await CreateProjectBuilder()
              .WithSourceCode(comment)
              .ShouldReportDiagnosticWithMessage($"TODO {todo}")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("/*[||]TODO*/", "")]
    [InlineData("/* [||]TODO*/", "")]
    [InlineData("/*[||]TODO test*/", "test")]
    [InlineData("/* [||]TODO test*/", "test")]
    [InlineData("  /* [||]TODO test*/", "test")]
    [InlineData("/*\n* [||]TODO test\r\n*/", "test")]
    public async Task MultiLinesComment(string comment, string todo)
    {
        await CreateProjectBuilder()
              .WithSourceCode(comment)
              .ShouldReportDiagnosticWithMessage($"TODO {todo}")
              .ValidateAsync();
    }

    [Fact]
    public async Task MultiTodoComment()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
/*
 * [||]TODO a
 * [||]TODO b
 */")
              .ShouldReportDiagnosticWithMessage("TODO a")
              .ShouldReportDiagnosticWithMessage("TODO b")
              .ValidateAsync();
    }
}

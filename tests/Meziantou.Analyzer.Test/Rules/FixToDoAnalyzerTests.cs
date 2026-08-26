namespace Meziantou.Analyzer.Test.Rules;

public sealed class FixToDoAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<FixToDoAnalyzer>();
    }

    [Theory]
    [InlineData("//")]
    [InlineData("//test")]
    [InlineData("//TOD")]
    [InlineData("//TODOA")]
    [InlineData("//TODO-A")]
    [InlineData("// (TODO)")]
    public async Task SingleLineCommentWithoutTodo(string comment)
    {
        await CreateProjectBuilder()
              .WithSourceCode(comment)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("//[|TODO|]", "")]
    [InlineData("// [|todo|]", "")]
    [InlineData("// [|ToDo|]", "")]
    [InlineData("// [|TODo|]", "")]
    [InlineData("// [|TODo?|]", "")]
    [InlineData("// [|TODo!|]", "")]
    [InlineData("//[|TODO test|]", "test")]
    [InlineData("// [|TODO test|]", "test")]
    [InlineData("  // [|TODO test|]", "test")]
    [InlineData("  // [|TODO: test|]", "test")]
    public async Task SingleLineComment(string comment, string todo)
    {
        await CreateProjectBuilder()
              .WithSourceCode(comment)
              .ShouldReportDiagnosticWithMessage($"TODO {todo}")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("/*[|TODO|]*/", "")]
    [InlineData("/* [|TODO|]*/", "")]
    [InlineData("/*[|TODO test|]*/", "test")]
    [InlineData("/* [|TODO test|]*/", "test")]
    [InlineData("  /* [|TODO test|]*/", "test")]
    [InlineData("/*\n* [|TODO test|]\r\n*/", "test")]
    public async Task MultiLinesComment(string comment, string todo)
    {
        await CreateProjectBuilder()
              .WithSourceCode(comment)
              .ShouldReportDiagnosticWithMessage($"TODO {todo}")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("/*")]
    [InlineData("/*a")]
    [InlineData("/*/")]
    [InlineData("/*ab")]
    [InlineData("/*test")]
    public async Task UnterminatedMultiLinesCommentWithoutTodo(string comment)
    {
        await CreateProjectBuilder()
              .WithSourceCode(comment)
              .WithNoCompilation()
              .ValidateAsync();
    }

    [Theory]
    [InlineData("/*[|TODO|]", "")]
    [InlineData("/* [|TODO|]", "")]
    [InlineData("/*[|TODO test|]", "test")]
    [InlineData("/* [|TODO test|]", "test")]
    [InlineData("/*\n* [|TODO test|]", "test")]
    public async Task UnterminatedMultiLinesComment(string comment, string todo)
    {
        await CreateProjectBuilder()
              .WithSourceCode(comment)
              .WithNoCompilation()
              .ShouldReportDiagnosticWithMessage($"TODO {todo}")
              .ValidateAsync();
    }

    [Fact]
    public async Task MultiTodoComment()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
              /*
               * [|TODO a|]
               * [|TODO: b|]
               */
              """)
              .ShouldReportDiagnosticWithMessage("TODO a")
              .ShouldReportDiagnosticWithMessage("TODO b")
              .ValidateAsync();
    }
}

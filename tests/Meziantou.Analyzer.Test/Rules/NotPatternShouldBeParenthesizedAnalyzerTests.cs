using Microsoft.CodeAnalysis;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.NotPatternShouldBeParenthesizedAnalyzer,
    Meziantou.Analyzer.Rules.NotPatternShouldBeParenthesizedCodeFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class NotPatternShouldBeParenthesizedAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task Not_Null()
    {
        var test = CreateTest();
        test.TestCode = """
            string a = default;
            _ = a is not null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Not_Null_Or_Empty()
    {
        var test = CreateTest();
        test.TestCode = """
            string a = default;
            _ = a is [|not null|] or "";
            """;
        test.FixedCode = """
            string a = default;
            _ = a is (not null) or "";
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Not_Null_And_Empty()
    {
        var test = CreateTest();
        test.TestCode = """
            string a = default;
            _ = a is not null and "";
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Not_Or_GreaterThan()
    {
        var test = CreateTest();
        test.TestCode = """
            int a = default;
            _ = a is [|not 1|] or > 2;
            """;
        test.FixedCode = """
            int a = default;
            _ = a is (not 1) or > 2;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Parentheses_Not_Or_GreaterThan()
    {
        var test = CreateTest();
        test.TestCode = """
            int a = 1;
            _ = a is (not 1) or > 2;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GreaterThan_Or_Not()
    {
        var test = CreateTest();
        test.TestCode = """
            int a = 1;
            _ = a is 1 or not (< 0);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GreaterThan_Or_Not_Or_Not()
    {
        var test = CreateTest();
        test.TestCode = """
            int a = 1;
            _ = a is 1 or not < 0 or not > 1;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Not_Many_or_Fix1()
    {
        var test = CreateTest();
        test.TestCode = """
            int a = 1;
            _ = a is [|not 1|] or 2 or 3 or 4;
            """;
        test.FixedCode = """
            int a = 1;
            _ = a is (not 1) or 2 or 3 or 4;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Not_Many_or_Fix2()
    {
        var test = CreateTest();
        test.TestCode = """
            int a = 1;
            _ = a is [|not 1|] or 2 or 3 or 4;
            """;
        test.FixedCode = """
            int a = 1;
            _ = a is not (1 or 2 or 3 or 4);
            """;
        test.CodeActionIndex = 1;

        return test.RunAsync();
    }
}

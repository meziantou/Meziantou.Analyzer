using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.ExceptionNameShouldEndWithExceptionAnalyzer,
    Meziantou.Analyzer.Rules.TypeNameShouldEndWithSuffixFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ExceptionNameShouldEndWithExceptionAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task NameEndsWithException()
    {
        var test = CreateTest();
        test.TestCode = """
            class CustomException : System.Exception
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NameDoesNotEndWithAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            class [|CustomEx|] : System.Exception
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NameDoesNotEndWithAttribute_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class [|CustomEx|] : System.Exception
            {
            }
            """;
        test.FixedCode = """
            class CustomExException : System.Exception
            {
            }
            """;

        return test.RunAsync();
    }
}

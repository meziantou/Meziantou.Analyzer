using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.EventArgsNameShouldEndWithEventArgsAnalyzer,
    Meziantou.Analyzer.Rules.TypeNameShouldEndWithSuffixFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EventArgsNameShouldEndWithEventArgsAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task NameEndsWithEventArgs()
    {
        var test = CreateTest();
        test.TestCode = """
            class CustomEventArgs : System.EventArgs
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NameDoesNotEndWithEventArgs()
    {
        var test = CreateTest();
        test.TestCode = """
            class [|CustomArgs|] : System.EventArgs
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NameDoesNotEndWithEventArgs_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class [|CustomArgs|] : System.EventArgs
            {
            }
            """;
        test.FixedCode = """
            class CustomArgsEventArgs : System.EventArgs
            {
            }
            """;

        return test.RunAsync();
    }
}

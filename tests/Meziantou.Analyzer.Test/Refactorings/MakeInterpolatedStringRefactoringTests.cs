using Microsoft.CodeAnalysis;
using RefactoringTest = Meziantou.Analyzer.Test.Harness.CSharpCodeRefactoringTest<
    Meziantou.Analyzer.Refactorings.MakeInterpolatedStringRefactoring>;

namespace Meziantou.Analyzer.Test.Refactorings;

public sealed class MakeInterpolatedStringRefactoringTests
{
    private static RefactoringTest CreateTest()
    {
        var test = new RefactoringTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task SimpleString()
    {
        var test = CreateTest();
        test.TestCode = """_ = [|"test"|];""";
        test.FixedCode = """_ = $"test";""";

        return test.RunAsync();
    }

    [Fact]
    public Task VerbatimString()
    {
        var test = CreateTest();
        test.TestCode = """_ = [|@"test"|];""";
        test.FixedCode = """_ = $@"test";""";

        return test.RunAsync();
    }

    [Fact]
    public Task SimpleStringWithOpenAndCloseCurlyBraces()
    {
        var test = CreateTest();
        test.TestCode = """_ = [|"test{0}"|];""";
        test.FixedCode = """_ = $"test{0}";""";

        return test.RunAsync();
    }

    [Fact]
    public Task VerbatimStringWithOpenAndCloseCurlyBraces()
    {
        var test = CreateTest();
        test.TestCode = """_ = [|@"test{0}"|];""";
        test.FixedCode = """_ = $@"test{0}";""";

        return test.RunAsync();
    }
}

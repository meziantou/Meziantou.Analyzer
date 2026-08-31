using Microsoft.CodeAnalysis;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.ObjectGetTypeOnTypeInstanceAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ObjectGetTypeOnTypeInstanceAnalyzerTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Theory]
    [InlineData("new object().GetType();")]
    [InlineData("string.Empty.GetType();")]
    [InlineData("12.GetType();")]
    [InlineData("System.Type.GetType(\"\");")]
    public Task Valid(string code)
    {
        var test = CreateTest();
        test.TestCode = code;

        return test.RunAsync();
    }

    [Fact]
    public Task AbstractClass_Valid()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            abstract class Test
            {
                public Test(int a) { }
            };
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("new object().GetType().GetType();")]
    [InlineData("((System.Type)null).GetType();")]
    [InlineData("default(System.Type).GetType();")]
    public Task Invalid(string code)
    {
        var test = CreateTest();
        test.TestCode = code;

        return test.RunAsync();
    }
}

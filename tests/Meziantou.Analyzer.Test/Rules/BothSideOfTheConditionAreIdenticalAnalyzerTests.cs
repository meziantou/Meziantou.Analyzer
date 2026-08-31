using Microsoft.CodeAnalysis;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.BothSideOfTheConditionAreIdenticalAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class BothSideOfTheConditionAreIdenticalAnalyzerTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Theory]
    [InlineData("a == b")]
    [InlineData("a != b")]
    [InlineData("a & b")]
    [InlineData("a && b")]
    [InlineData("a | b")]
    [InlineData("a || b")]
    [InlineData("a is false")]
    [InlineData("a is true")]
    [InlineData("a is false or true")]
    [InlineData("a is false and not true")]
    public Task DifferentCode(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            var a = false;
            var b = false;
            var c = 0;
            _ = {{expression}};
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("{|MA0172:a == a|}")]
    [InlineData("{|MA0172:a != a|}")]
    [InlineData("{|MA0172:a & a|}")]
    [InlineData("{|MA0172:a && a|}")]
    [InlineData("{|MA0172:a | a|}")]
    [InlineData("{|MA0172:a || a|}")]
    [InlineData("a is {|MA0172:true or true|}")]
    [InlineData("a is {|MA0172:true and true|}")]
    public Task SameCode(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            var a = false;
            var b = false;
            var c = 0;
            _ = {{expression}};
            """;

        return test.RunAsync();
    }
}

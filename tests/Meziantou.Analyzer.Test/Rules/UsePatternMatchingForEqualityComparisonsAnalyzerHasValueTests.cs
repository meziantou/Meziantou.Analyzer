using Microsoft.CodeAnalysis;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UsePatternMatchingInsteadOfHasValueAnalyzer,
    Meziantou.Analyzer.Rules.UsePatternMatchingInsteadOfHasvalueFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UsePatternMatchingForEqualityComparisonsAnalyzerHasValueTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task HasValue()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = default(int?);
            _ = [|value.HasValue|];
            """;
        test.FixedCode = """
            var value = default(int?);
            _ = value is not null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotHasValue()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = default(int?);
            _ = ![|value.HasValue|];
            """;
        test.FixedCode = """
            var value = default(int?);
            _ = value is null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasValueEqualsTrue()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = default(int?);
            _ = [|value.HasValue|] == true;
            """;
        test.FixedCode = """
            var value = default(int?);
            _ = value is not null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasValueEqualsFalse()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = default(int?);
            _ = [|value.HasValue|] == false;
            """;
        test.FixedCode = """
            var value = default(int?);
            _ = value is null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FalseEqualsHasValue()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = default(int?);
            _ = false == [|value.HasValue|];
            """;
        test.FixedCode = """
            var value = default(int?);
            _ = value is null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasValueIsTrue()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = default(int?);
            _ = [|value.HasValue|] is true;
            """;
        test.FixedCode = """
            var value = default(int?);
            _ = value is not null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasValueIsFalse()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = default(int?);
            _ = [|value.HasValue|] is false;
            """;
        test.FixedCode = """
            var value = default(int?);
            _ = value is null;
            """;

        return test.RunAsync();
    }
}

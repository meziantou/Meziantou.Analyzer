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
            _ = {|MA0171:value.HasValue|};
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
            _ = !{|MA0171:value.HasValue|};
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
            _ = {|MA0171:value.HasValue|} == true;
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
            _ = {|MA0171:value.HasValue|} == false;
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
            _ = false == {|MA0171:value.HasValue|};
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
            _ = {|MA0171:value.HasValue|} is true;
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
            _ = {|MA0171:value.HasValue|} is false;
            """;
        test.FixedCode = """
            var value = default(int?);
            _ = value is null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasValue_MemberAccessReceiver()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = default(int?);
            _ = {|MA0171:value.HasValue|}.ToString();
            """;
        test.FixedCode = """
            var value = default(int?);
            _ = (value is not null).ToString();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasValueEqualsFalse_MemberAccessReceiver()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = default(int?);
            _ = ({|MA0171:value.HasValue|} == false).ToString();
            """;
        test.FixedCode = """
            var value = default(int?);
            _ = (value is null).ToString();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasValue_Cast()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = default(int?);
            _ = (object){|MA0171:value.HasValue|};
            """;
        test.FixedCode = """
            var value = default(int?);
            _ = (object)(value is not null);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasValue_Nameof()
    {
        var test = CreateTest();
        test.TestCode = """
            int? value = null;
            _ = nameof(value.HasValue);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasValue_ConditionalAccess()
    {
        var test = CreateTest();
        test.TestCode = """
            A a = null;
            _ = a?.B.HasValue;
            _ = a?.B.HasValue.ToString();
            _ = a?.B.HasValue.GetType();

            class A
            {
                public int? B;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasValue_ConditionalAccessExtensionMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            A a = null;
            _ = a?.B.HasValue.Negate();

            class A
            {
                public int? B;
            }

            static class Extensions
            {
                public static bool Negate(this bool value) => !value;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasValue_ParenthesizedConditionalAccess()
    {
        var test = CreateTest();
        test.TestCode = """
            A a = null;
            _ = {|MA0171:(a?.B).HasValue|};

            class A
            {
                public int? B;
            }
            """;
        test.FixedCode = """
            A a = null;
            _ = (a?.B) is not null;

            class A
            {
                public int? B;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasValue_ArgumentOfConditionalAccess()
    {
        var test = CreateTest();
        test.TestCode = """
            A a = null;
            int? value = null;
            _ = a?.M({|MA0171:value.HasValue|});

            class A
            {
                public int M(bool value) => 0;
            }
            """;
        test.FixedCode = """
            A a = null;
            int? value = null;
            _ = a?.M(value is not null);

            class A
            {
                public int M(bool value) => 0;
            }
            """;

        return test.RunAsync();
    }
}

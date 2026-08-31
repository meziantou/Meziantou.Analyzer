using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UsePatternMatchingForEqualityComparisonsAnalyzer,
    Meziantou.Analyzer.Rules.UsePatternMatchingForEqualityComparisonsFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UsePatternMatchingForEqualityComparisonsAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task DisabledInExpression()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq;
            using System.Linq.Expressions;
            _ = (Expression<Func<int, bool>>)(item => item == 0);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NullCheckForNullableOfT()
    {
        var test = CreateTest();
        test.TestCode = "_ = {|MA0142:(int?)0 == null|};";
        test.FixedCode = "_ = (int?)0 is null;";

        return test.RunAsync();
    }

    [Fact]
    public Task NullCheckForNullableOfT_NotNull()
    {
        var test = CreateTest();
        test.TestCode = "_ = {|MA0141:(int?)0 != null|};";
        test.FixedCode = "_ = (int?)0 is not null;";

        return test.RunAsync();
    }

    [Fact]
    public Task NullCheckForObject()
    {
        var test = CreateTest();
        test.TestCode = "_ = {|MA0142:new object() == null|};";
        test.FixedCode = "_ = new object() is null;";

        return test.RunAsync();
    }

    [Fact]
    public Task NullCheckForObject_NullFirst()
    {
        var test = CreateTest();
        test.TestCode = "_ = {|MA0142:null == new object()|};";
        test.FixedCode = "_ = new object() is null;";

        return test.RunAsync();
    }

    [Fact]
    public Task NullCheckForObject_NotNull_NullFirst()
    {
        var test = CreateTest();
        test.TestCode = "_ = {|MA0141:null != new object()|};";
        test.FixedCode = "_ = new object() is not null;";

        return test.RunAsync();
    }

    [Fact]
    public Task NullCheckForObject_FixerKeepParentheses()
    {
        var test = CreateTest();
        test.TestCode = """
            string line;
            while ({|MA0141:(line = null) != null|}) { }
            """;
        test.FixedCode = """
            string line;
            while ((line = null) is not null) { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NullEqualsNull()
    {
        // no report as "null is null" is not valid
        var test = CreateTest();
        test.TestCode = "_ = null == null;";

        return test.RunAsync();
    }

    [Fact]
    public Task NotNullCheck()
    {
        // no report as "null is null" is not valid
        var test = CreateTest();
        test.TestCode = "_ = new object() == new object();";

        return test.RunAsync();
    }

    [Fact]
    public Task NullCheckForObjectWithCustomOperator()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = new Sample() == null;

            class Sample
            {
                public static bool operator ==(Sample left, Sample right) => false;
                public static bool operator !=(Sample left, Sample right) => false;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NullCheckForNullableOfT_IsNull()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = (int?)0 is null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EqualityComparison_String()
    {
        var test = CreateTest();
        test.TestCode = """_ = {|MA0148:(string)"dummy" == "dummy"|};""";
        test.FixedCode = """_ = (string)"dummy" is "dummy";""";

        return test.RunAsync();
    }

    [Fact]
    public Task EqualityComparison_NullableInt32_Int32()
    {
        var test = CreateTest();
        test.TestCode = "_ = {|MA0148:(int?)0 == 1|};";
        test.FixedCode = "_ = (int?)0 is 1;";

        return test.RunAsync();
    }

    [Fact]
    public Task EqualityComparison_Enum()
    {
        var test = CreateTest();
        test.TestCode = "_ = {|MA0148:(System.DayOfWeek)1 == System.DayOfWeek.Monday|};";
        // "x is System.DayOfWeek.Monday" is parsed as a type check, whereas the fixer builds the constant
        // pattern the compiler binds it to, so the shape of the tree cannot be compared with the parsed one
        test.CodeActionValidationMode = CodeActionValidationMode.None;
        test.FixedCode = "_ = (System.DayOfWeek)1 is System.DayOfWeek.Monday;";

        return test.RunAsync();
    }

    [Fact]
    public Task EqualityComparison_NullableEnum()
    {
        var test = CreateTest();
        test.TestCode = "_ = {|MA0148:(System.DayOfWeek?)1 == System.DayOfWeek.Monday|};";
        // "x is System.DayOfWeek.Monday" is parsed as a type check, whereas the fixer builds the constant
        // pattern the compiler binds it to, so the shape of the tree cannot be compared with the parsed one
        test.CodeActionValidationMode = CodeActionValidationMode.None;
        test.FixedCode = "_ = (System.DayOfWeek?)1 is System.DayOfWeek.Monday;";

        return test.RunAsync();
    }

    [Fact]
    public Task EqualityComparison_MergeConditions()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = 0;
            _ = {|MA0148:value == 0|} || {|MA0148:value == 1|};
            """;
        test.FixedCode = """
            var value = 0;
            _ = value is 0 or 1;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InequalityComparison_MergeConditions()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = 0;
            _ = {|MA0149:value != 0|} && {|MA0149:value != 1|};
            """;
        test.FixedCode = """
            var value = 0;
            _ = value is not (0 or 1);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EqualityComparison_DifferentExpressions_DoNotMerge()
    {
        var test = CreateTest();
        test.TestCode = """
            var value1 = 0;
            var value2 = 0;
            _ = {|MA0148:value1 == 0|} || {|MA0148:value2 == 1|};
            """;
        test.FixedCode = """
            var value1 = 0;
            var value2 = 0;
            _ = value1 is 0 || value2 is 1;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EqualityComparison_NonContiguousExpressions_DoNotMerge()
    {
        var test = CreateTest();
        test.TestCode = """
            var value1 = 0;
            var value2 = 0;
            _ = {|MA0148:value1 == 0|} || {|MA0148:value2 == 1|} || {|MA0148:value1 == 2|};
            """;
        test.FixedCode = """
            var value1 = 0;
            var value2 = 0;
            _ = value1 is 0 || value2 is 1 || value1 is 2;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task BatchFix_MergeConditions()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = 0;
            _ = {|MA0148:value == 0|} || {|MA0148:value == 1|};
            _ = {|MA0149:value != 2|} && {|MA0149:value != 3|};
            """;
        test.FixedCode = """
            var value = 0;
            _ = value is 0 or 1;
            _ = value is not (2 or 3);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CustomOperator_Class_Int32()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = new Sample() == 1;

            class Sample
            {
                public static bool operator ==(Sample left, int right) => false;
                public static bool operator !=(Sample left, int right) => false;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EqualityComparison_ImplicitConversion_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample value = null;
            _ = value == 0;

            class Sample
            {
                public static implicit operator int(Sample value) => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InequalityComparison_ImplicitConversion_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample value = null;
            _ = value != 0;

            class Sample
            {
                public static implicit operator int(Sample value) => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EqualityComparison_MixedWithImplicitConversion_OnlyFixValidExpression()
    {
        var test = CreateTest();
        test.TestCode = """
            var number = 0;
            Sample value = null;
            _ = {|MA0148:number == 0|} || value == 0;

            class Sample
            {
                public static implicit operator int(Sample value) => 0;
            }
            """;
        test.FixedCode = """
            var number = 0;
            Sample value = null;
            _ = number is 0 || value == 0;

            class Sample
            {
                public static implicit operator int(Sample value) => 0;
            }
            """;

        return test.RunAsync();
    }
}

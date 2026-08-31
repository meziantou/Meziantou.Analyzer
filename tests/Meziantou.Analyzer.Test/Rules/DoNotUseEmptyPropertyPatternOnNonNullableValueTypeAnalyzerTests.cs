using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.DoNotUseEmptyPropertyPatternOnNonNullableValueTypeAnalyzer,
    Meziantou.Analyzer.Rules.DoNotUseEmptyPropertyPatternOnNonNullableValueTypeFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseEmptyPropertyPatternOnNonNullableValueTypeAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task IsEmptyPropertyPattern_NonNullableValueType_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static bool A()
                {
                    int value = 0;
                    return value is {|MA0200:{ }|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsEmptyPropertyPattern_ConstrainedGenericValueType_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static bool A<T>(T value) where T : struct => value is {|MA0200:{ }|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsEmptyPropertyPatternWithDesignation_NonNullableValueType_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static bool A()
                {
                    int value = 0;
                    return value is {|MA0200:{ } newName|};
                }
            }
            """;
        test.FixedCode = """
            class Sample
            {
                private static bool A()
                {
                    int value = 0;
                    return value is var newName;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsEmptyPropertyPattern_NestedPropertyPattern_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private sealed class Nested
                {
                    public int Value { get; set; }
                }

                private static bool A(Nested value)
                {
                    return value is { Value: {|MA0200:{ }|} };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsEmptyPropertyPatternWithDesignation_NestedPropertyPattern_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private sealed class Nested
                {
                    public int Value { get; set; }
                }

                private static bool A(Nested value)
                {
                    return value is { Value: {|MA0200:{ } newName|} };
                }
            }
            """;
        test.FixedCode = """
            class Sample
            {
                private sealed class Nested
                {
                    public int Value { get; set; }
                }

                private static bool A(Nested value)
                {
                    return value is { Value: var newName };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsEmptyPropertyPatternWithDesignation_MultipleNestedPropertyPatterns_BatchCodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private sealed class Nested
                {
                    public int Value1 { get; set; }
                    public int Value2 { get; set; }
                }

                private static bool A(Nested value)
                {
                    return value is { Value1: {|MA0200:{ } name1|}, Value2: {|MA0200:{ } name2|} };
                }
            }
            """;
        test.FixedCode = """
            class Sample
            {
                private sealed class Nested
                {
                    public int Value1 { get; set; }
                    public int Value2 { get; set; }
                }

                private static bool A(Nested value)
                {
                    return value is { Value1: var name1, Value2: var name2 };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsEmptyPropertyPattern_NullableValueType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static bool A()
                {
                    int? value = 0;
                    return value is { };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsNotEmptyPropertyPattern_NullableValueType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static bool A()
                {
                    int? value = 0;
                    return value is not { };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsEmptyPropertyPattern_ReferenceType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static bool A()
                {
                    object value = 0;
                    return value is { };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsEmptyPropertyPattern_UnconstrainedGenericType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static bool A<T>(T value) => value is { };
            }
            """;

        return test.RunAsync();
    }
}

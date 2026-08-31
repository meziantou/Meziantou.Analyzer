using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.SimplifyNegatedBooleanExpressionAnalyzer,
    Meziantou.Analyzer.Rules.SimplifyNegatedBooleanExpressionFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class SimplifyNegatedBooleanExpressionAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task Issue1264()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(char key, object target)
                {
                    if ([|!(key == 'y' && !IsEditable(target))|])
                    {
                    }
                }

                bool IsEditable(object target) => true;
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test(char key, object target)
                {
                    if (key != 'y' || IsEditable(target))
                    {
                    }
                }

                bool IsEditable(object target) => true;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DoubleNegation()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(bool a, bool b)
                {
                    _ = [|!(!a && !b)|];
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test(bool a, bool b)
                {
                    _ = a || b;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LogicalOr()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(bool a, int b)
                {
                    _ = [|!(!a || b == 0)|];
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test(bool a, int b)
                {
                    _ = a && b != 0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RelationalComparisonsWithoutInnerNegation_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(int value, int min, int max)
                {
                    _ = !(value < max && value >= min);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReturnExpression()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                bool Test(bool a, bool b)
                {
                    return [|!(a || !b)|];
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                bool Test(bool a, bool b)
                {
                    return !a && b;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParentContextPrecedence()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(bool a, bool b, bool c)
                {
                    _ = a && [|!(b && !c)|];
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test(bool a, bool b, bool c)
                {
                    _ = a && (!b || c);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ChildPrecedence()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(bool a, bool b, bool c)
                {
                    _ = [|!(!(a || b) || c)|];
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test(bool a, bool b, bool c)
                {
                    _ = (a || b) && !c;
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("!(x && y)")]
    [InlineData("!(x || y)")]
    [InlineData("!(x && a == 0)")]
    [InlineData("!(a == 0 && b == 0)")]
    public Task PlainNegatedGroups_NoDiagnostic(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                void Test(bool x, bool y, int a, int b)
                {
                    _ = {{expression}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DynamicOperand_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(dynamic a, bool b)
                {
                    _ = !(a && !b);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UserDefinedOperator_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct TestBoolean
            {
                public static TestBoolean operator &(TestBoolean left, TestBoolean right) => left;
                public static bool operator true(TestBoolean value) => true;
                public static bool operator false(TestBoolean value) => false;
                public static bool operator !(TestBoolean value) => true;
            }

            class TestClass
            {
                void Test(TestBoolean a, TestBoolean b)
                {
                    _ = !(a && b);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NullableRelationalComparison_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(int? a, int? b)
                {
                    _ = !(a < b && a != b);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FloatingPointRelationalComparison_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(double value, double max)
                {
                    _ = !(value < max && value != max);
                }
            }
            """;

        return test.RunAsync();
    }
}

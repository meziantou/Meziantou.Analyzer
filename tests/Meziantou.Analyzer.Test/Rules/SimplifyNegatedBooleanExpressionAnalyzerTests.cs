using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class SimplifyNegatedBooleanExpressionAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<SimplifyNegatedBooleanExpressionAnalyzer>()
            .WithCodeFixProvider<SimplifyNegatedBooleanExpressionFixer>();
    }

    [Fact]
    public async Task Issue1264()
    {
        const string SourceCode = """
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

        const string FixedCode = """
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

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(FixedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DoubleNegation()
    {
        const string SourceCode = """
            class TestClass
            {
                void Test(bool a, bool b)
                {
                    _ = [|!(!a && !b)|];
                }
            }
            """;

        const string FixedCode = """
            class TestClass
            {
                void Test(bool a, bool b)
                {
                    _ = a || b;
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(FixedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LogicalOr()
    {
        const string SourceCode = """
            class TestClass
            {
                void Test(bool a, int b)
                {
                    _ = [|!(!a || b == 0)|];
                }
            }
            """;

        const string FixedCode = """
            class TestClass
            {
                void Test(bool a, int b)
                {
                    _ = a && b != 0;
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(FixedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RelationalComparisonsWithoutInnerNegation_NoDiagnostic()
    {
        const string SourceCode = """
            class TestClass
            {
                void Test(int value, int min, int max)
                {
                    _ = !(value < max && value >= min);
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReturnExpression()
    {
        const string SourceCode = """
            class TestClass
            {
                bool Test(bool a, bool b)
                {
                    return [|!(a || !b)|];
                }
            }
            """;

        const string FixedCode = """
            class TestClass
            {
                bool Test(bool a, bool b)
                {
                    return !a && b;
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(FixedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ParentContextPrecedence()
    {
        const string SourceCode = """
            class TestClass
            {
                void Test(bool a, bool b, bool c)
                {
                    _ = a && [|!(b && !c)|];
                }
            }
            """;

        const string FixedCode = """
            class TestClass
            {
                void Test(bool a, bool b, bool c)
                {
                    _ = a && (!b || c);
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(FixedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ChildPrecedence()
    {
        const string SourceCode = """
            class TestClass
            {
                void Test(bool a, bool b, bool c)
                {
                    _ = [|!(!(a || b) || c)|];
                }
            }
            """;

        const string FixedCode = """
            class TestClass
            {
                void Test(bool a, bool b, bool c)
                {
                    _ = (a || b) && !c;
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(FixedCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("!(x && y)")]
    [InlineData("!(x || y)")]
    [InlineData("!(x && a == 0)")]
    [InlineData("!(a == 0 && b == 0)")]
    public async Task PlainNegatedGroups_NoDiagnostic(string expression)
    {
        var sourceCode = $$"""
            class TestClass
            {
                void Test(bool x, bool y, int a, int b)
                {
                    _ = {{expression}};
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DynamicOperand_NoDiagnostic()
    {
        const string SourceCode = """
            class TestClass
            {
                void Test(dynamic a, bool b)
                {
                    _ = !(a && !b);
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UserDefinedOperator_NoDiagnostic()
    {
        const string SourceCode = """
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

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NullableRelationalComparison_NoDiagnostic()
    {
        const string SourceCode = """
            class TestClass
            {
                void Test(int? a, int? b)
                {
                    _ = !(a < b && a != b);
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FloatingPointRelationalComparison_NoDiagnostic()
    {
        const string SourceCode = """
            class TestClass
            {
                void Test(double value, double max)
                {
                    _ = !(value < max && value != max);
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}

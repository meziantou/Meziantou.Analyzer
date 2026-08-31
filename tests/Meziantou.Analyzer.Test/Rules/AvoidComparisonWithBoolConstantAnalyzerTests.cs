using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.AvoidComparisonWithBoolConstantAnalyzer,
    Meziantou.Analyzer.Rules.AvoidComparisonWithBoolConstantFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AvoidComparisonWithBoolConstantAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Theory]
    [InlineData("==", "true", null)]
    [InlineData("==", "false", "!")]
    [InlineData("!=", "true", "!")]
    [InlineData("!=", "false", null)]
    public Task ComparingVariableWithBoolLiteral_RemovesComparisonAndKeepsVariable(string op, string literal, string? expectedPrefix)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                void Test()
                {
                    var value = false;
                    if (value [|{{op}}|] {{literal}})
                    {
                    }
                }
            }
            """;
        test.FixedCode = $$"""
            class TestClass
            {
                void Test()
                {
                    var value = false;
                    if ({{expectedPrefix}}value)
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("true", "==", "(GetSomeNumber() == 15)", "GetSomeNumber() == 15")]
    [InlineData("false", "==", "(GetSomeNumber() == 15)", "!(GetSomeNumber() == 15)")]
    [InlineData("true", "!=", "(GetSomeNumber() == 15)", "!(GetSomeNumber() == 15)")]
    [InlineData("false", "!=", "(GetSomeNumber() == 15)", "GetSomeNumber() == 15")]
    public Task ComparingBoolLiteralWithExpression_RemovesComparisonAndKeepsExpression(string literal, string op, string originalExpression, string modifiedExpression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                void Test()
                {
                    var value = {{literal}} [|{{op}}|] {{originalExpression}};
                    int GetSomeNumber() => 12;
                }
            }
            """;
        test.FixedCode = $$"""
            class TestClass
            {
                void Test()
                {
                    var value = {{modifiedExpression}};
                    int GetSomeNumber() => 12;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsPatternComparedWithFalse_ParenthesizesExpression()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(object o)
                {
                    _ = o is string [|==|] false;
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test(object o)
                {
                    _ = !(o is string);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsPatternComparedWithTrue_KeepsExpression()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(object o)
                {
                    _ = o is string [|==|] true;
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test(object o)
                {
                    _ = o is string;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsPatternNotEqualToTrue_ParenthesizesExpression()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(object o)
                {
                    _ = o is string [|!=|] true;
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test(object o)
                {
                    _ = !(o is string);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsPatternNotEqualToFalse_KeepsExpression()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(object o)
                {
                    _ = o is string [|!=|] false;
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test(object o)
                {
                    _ = o is string;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ComparisonComparedWithFalse_ParenthesizesExpression()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(int a, int b)
                {
                    _ = a < b [|==|] false;
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test(int a, int b)
                {
                    _ = !(a < b);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ComparisonComparedWithTrue_KeepsExpression()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(int a, int b)
                {
                    _ = a < b [|==|] true;
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test(int a, int b)
                {
                    _ = a < b;
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("==", "true", null)]
    [InlineData("==", "false", "!")]
    [InlineData("!=", "true", "!")]
    [InlineData("!=", "false", null)]
    public Task ComparingVariableWithBoolConstant_RemovesComparisonAndKeepsVariable(string op, string constBool, string? expectedPrefix)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                void Test()
                {
                    const bool MyConstant = {{constBool}};
                    bool value = false;
                    _ = value [|{{op}}|] MyConstant;
                }
            }
            """;
        test.FixedCode = $$"""
            class TestClass
            {
                void Test()
                {
                    const bool MyConstant = {{constBool}};
                    bool value = false;
                    _ = {{expectedPrefix}}value;
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("!=", "true", "!")]
    [InlineData("==", "MyConstant2", null)]
    public Task ComparingBoolConstantsAndLiterals_RemovesComparisonAndKeepsRightOperand(string op, string rightOperand, string? expectedPrefix)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                void Test()
                {
                    const bool MyConstant1 = true;
                    const bool MyConstant2 = false;
                    _ = MyConstant1 [|{{op}}|] {{rightOperand}};
                }
            }
            """;
        test.FixedCode = $$"""
            class TestClass
            {
                void Test()
                {
                    const bool MyConstant1 = true;
                    const bool MyConstant2 = false;
                    _ = {{expectedPrefix}}{{rightOperand}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ComparingNullableBoolVariableWithBoolLiteral_NoDiagnosticReported()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    bool? value = true;
                    if (value == true)
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("dynamicValue == true")]
    [InlineData("true == AsDynamic().MaybeBoolean")]
    [InlineData("((dynamic)this.TrulyBoolean) == true")]
    public Task ComparingDynamicVariableWithBoolLiteral_NoDiagnosticReported(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                public bool? MaybeBoolean { get; set; }
                public bool  TrulyBoolean { get; set; }

                public dynamic AsDynamic() { return this; }

                void Test()
                {
                    dynamic dynamicValue = true;
                    if ({{expression}})
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotComparingBoolVariable_NoDiagnosticReported()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    bool value = true;
                    if (value)
                    {
                    }
                    if (!value)
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ComparingNullableLongVariableWithNullLiteral_NoDiagnosticReported()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(long? number)
                {
                    if (number == null)
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }
}

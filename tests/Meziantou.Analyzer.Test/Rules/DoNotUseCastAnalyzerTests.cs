using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseCastAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseCastAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task ExplicitCast_IntToDouble_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    int value = 42;
                    double result = [|(double)value|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitCast_ObjectToString_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    object value = "test";
                    string result = [|(string)value|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitCast_EnumToInt_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            enum MyEnum { Value1, Value2 }

            class TestClass
            {
                void Test()
                {
                    MyEnum value = MyEnum.Value1;
                    int result = [|(int)value|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitCast_IntToEnum_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            enum MyEnum { Value1, Value2 }

            class TestClass
            {
                void Test()
                {
                    int value = 1;
                    MyEnum result = [|(MyEnum)value|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitCast_CharToInt_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    char value = 'A';
                    int result = [|(int)value|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitCast_IntToChar_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    int value = 65;
                    char result = [|(char)value|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitConversion_IntToDouble_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    int value = 42;
                    double result = value;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AsOperator_ObjectToString_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    object value = "test";
                    string? result = value as string;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsPattern_ObjectToString_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    object value = "test";
                    if (value is string result)
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UserDefinedConversion_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class MyClass
            {
                public static explicit operator int(MyClass c) => 0;
            }

            class TestClass
            {
                void Test()
                {
                    MyClass value = new MyClass();
                    int result = (int)value;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitCast_MultipleInMethod_ShouldReportAll()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    int a = 1;
                    int b = 2;
                    double x = [|(double)a|];
                    double y = [|(double)b|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitCast_InExpression_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    int value = 42;
                    double result = [|(double)value|] + 1.5;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitCast_BaseToDerivedException_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class TestClass
            {
                void Test()
                {
                    Exception ex = new ArgumentException();
                    ArgumentException argEx = [|(ArgumentException)ex|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitCast_NullableToValue_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    int? nullable = 42;
                    int value = [|(int)nullable|];
                }
            }
            """;

        return test.RunAsync();
    }
}

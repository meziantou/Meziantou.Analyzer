using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseCastAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotUseCastAnalyzer>();
    }

    [Fact]
    public async Task ExplicitCast_IntToDouble_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class TestClass
                {
                    void Test()
                    {
                        int value = 42;
                        double result = [|(double)value|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitCast_ObjectToString_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class TestClass
                {
                    void Test()
                    {
                        object value = "test";
                        string result = [|(string)value|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitCast_EnumToInt_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                enum MyEnum { Value1, Value2 }

                class TestClass
                {
                    void Test()
                    {
                        MyEnum value = MyEnum.Value1;
                        int result = [|(int)value|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitCast_IntToEnum_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                enum MyEnum { Value1, Value2 }

                class TestClass
                {
                    void Test()
                    {
                        int value = 1;
                        MyEnum result = [|(MyEnum)value|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitCast_CharToInt_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class TestClass
                {
                    void Test()
                    {
                        char value = 'A';
                        int result = [|(int)value|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitCast_IntToChar_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class TestClass
                {
                    void Test()
                    {
                        int value = 65;
                        char result = [|(char)value|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ImplicitConversion_IntToDouble_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class TestClass
                {
                    void Test()
                    {
                        int value = 42;
                        double result = value;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task AsOperator_ObjectToString_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class TestClass
                {
                    void Test()
                    {
                        object value = "test";
                        string? result = value as string;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task IsPattern_ObjectToString_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task UserDefinedConversion_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitCast_MultipleInMethod_ShouldReportAll()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitCast_InExpression_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class TestClass
                {
                    void Test()
                    {
                        int value = 42;
                        double result = [|(double)value|] + 1.5;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitCast_BaseToDerivedException_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class TestClass
                {
                    void Test()
                    {
                        Exception ex = new ArgumentException();
                        ArgumentException argEx = [|(ArgumentException)ex|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitCast_NullableToValue_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class TestClass
                {
                    void Test()
                    {
                        int? nullable = 42;
                        int value = [|(int)nullable|];
                    }
                }
                """)
            .ValidateAsync();
    }
}

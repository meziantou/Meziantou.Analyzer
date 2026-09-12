using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseInKeywordForInParameterAnalyzer,
    Meziantou.Analyzer.Rules.UseInKeywordForInParameterFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseInKeywordForInParameterAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task StyleRule_Variable_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    var value = new S();
                    M({|MA0209:value|});
                }

                private static void M(in S value) { }
            }

            struct S { }
            """;
        test.FixedCode = """
            class C
            {
                public void Test()
                {
                    var value = new S();
                    M(in value);
                }

                private static void M(in S value) { }
            }

            struct S { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StyleRule_AlreadyIn_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    var value = new S();
                    M(in value);
                }

                private static void M(in S value) { }
            }

            struct S { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StyleRule_Literal_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    M(42);
                }

                private static void M(in int value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StyleRule_Property_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct S { }

            class C
            {
                public S Property => default;

                public void Test()
                {
                    M(Property);
                }

                private static void M(in S value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StyleRule_MethodReturnValue_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    M(GetValue());
                }

                private static S GetValue() => default;
                private static void M(in S value) { }
            }

            struct S { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StyleRule_ImplicitConversion_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    short value = 0;
                    M(value);
                }

                private static void M(in int value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StyleRule_Expression_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    var a = 1;
                    var b = 2;
                    M(a + b);
                }

                private static void M(in int value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StyleRule_ObjectCreation_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    M(new S());
                }

                private static void M(in S value) { }
            }

            struct S { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StyleRule_ConstantLocal_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    const int value = 1;
                    M(value);
                }

                private static void M(in int value) { }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("ConstantField")]
    [InlineData("E.A")]
    [InlineData("(S)Field")]
    [InlineData("GetS().Value")]
    [InlineData("Property.Value")]
    [InlineData("new S().Value")]
    [InlineData("default(S).Value")]
    [InlineData("GetS().Nested.Value")]
    public Task StyleRule_NotVariable_ShouldNotReportDiagnostic(string argument)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class C
            {
                private const int ConstantField = 1;
                private S Field;
                private static S Property => default;

                public void Test()
                {
                    M({{argument}});
                }

                private static S GetS() => default;
                private static void M<T>(in T value) { }
            }

            struct S
            {
                public int Value;
                public N Nested;
            }

            struct N
            {
                public int Value;
            }

            enum E { A }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StyleRule_ThisInClass_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    M(this);
                }

                private static void M(in C value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StyleRule_QueryRangeVariable_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;

            class C
            {
                public void Test()
                {
                    _ = from item in new[] { 1 } where M(item) select M(item);
                }

                private static bool M(in int value) => true;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Field", "in Field")]
    [InlineData("StaticReadOnlyField", "in StaticReadOnlyField")]
    [InlineData("Field.Value", "in Field.Value")]
    [InlineData("GetC().Field.Value", "in GetC().Field.Value")]
    [InlineData("GetArray()[0].Value", "in GetArray()[0].Value")]
    [InlineData("Field.Nested.Value", "in Field.Nested.Value")]
    public Task StyleRule_VariableMember_ShouldReportDiagnostic(string argument, string fixedArgument)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class C
            {
                private static readonly S StaticReadOnlyField;
                private S Field;

                public void Test()
                {
                    M({|MA0209:{{argument}}|});
                }

                private static C GetC() => new C();
                private static S[] GetArray() => new S[1];
                private static void M<T>(in T value) { }
            }

            struct S
            {
                public int Value;
                public N Nested;
            }

            struct N
            {
                public int Value;
            }
            """;
        test.FixedCode = $$"""
            class C
            {
                private static readonly S StaticReadOnlyField;
                private S Field;

                public void Test()
                {
                    M({{fixedArgument}});
                }

                private static C GetC() => new C();
                private static S[] GetArray() => new S[1];
                private static void M<T>(in T value) { }
            }

            struct S
            {
                public int Value;
                public N Nested;
            }

            struct N
            {
                public int Value;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StyleRule_ThisInStruct_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct S
            {
                public void Test()
                {
                    M({|MA0209:this|});
                }

                private static void M(in S value) { }
            }
            """;
        test.FixedCode = """
            struct S
            {
                public void Test()
                {
                    M(in this);
                }

                private static void M(in S value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StyleRule_LambdaParameter_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    System.Action<int> action = value => M({|MA0209:value|});
                }

                private static void M(in int value) { }
            }
            """;
        test.FixedCode = """
            class C
            {
                public void Test()
                {
                    System.Action<int> action = value => M(in value);
                }

                private static void M(in int value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OverloadRule_ConstantLocal_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    const int value = 1;
                    M(value);
                }

                private static void M(int value) { }
                private static void M(in int value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OverloadRule_Variable_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    var value = new S();
                    M({|MA0210:value|});
                }

                private static void M(S value) { }
                private static void M(in S value) { }
            }

            struct S { }
            """;
        test.FixedCode = """
            class C
            {
                public void Test()
                {
                    var value = new S();
                    M(in value);
                }

                private static void M(S value) { }
                private static void M(in S value) { }
            }

            struct S { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OverloadRule_Expression_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    M(new S());
                }

                private static void M(S value) { }
                private static void M(in S value) { }
            }

            struct S { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OverloadRule_ImplicitConversion_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    short value = 0;
                    M(value);
                }

                private static void M(int value) { }
                private static void M(in int value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OverloadRule_ReorderedNamedArguments_ShouldReportDiagnosticOnMatchingParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    int a = 1, b = 2;
                    M(y: b, {|MA0210:x: a|});
                }

                private static void M(int x, int y) { }
                private static void M(in int x, int y) { }
            }
            """;
        test.FixedCode = """
            class C
            {
                public void Test()
                {
                    int a = 1, b = 2;
                    M(y: b, x: in a);
                }

                private static void M(int x, int y) { }
                private static void M(in int x, int y) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OverloadRule_NamedArgumentsWithDifferentParameterNames_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    int a = 1, b = 2;
                    M(x: a, y: b);
                }

                private static void M(int x, int y) { }
                private static void M(in int y, int x) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OverloadRule_PositionalArgumentsWithDifferentParameterNames_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                public void Test()
                {
                    int a = 1, b = 2;
                    M({|MA0210:a|}, b);
                }

                private static void M(int x, int y) { }
                private static void M(in int y, int x) { }
            }
            """;
        test.FixedCode = """
            class C
            {
                public void Test()
                {
                    int a = 1, b = 2;
                    M(in a, b);
                }

                private static void M(int x, int y) { }
                private static void M(in int y, int x) { }
            }
            """;

        return test.RunAsync();
    }
}

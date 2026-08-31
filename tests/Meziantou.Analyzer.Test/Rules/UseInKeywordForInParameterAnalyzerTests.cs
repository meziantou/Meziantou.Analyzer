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
}

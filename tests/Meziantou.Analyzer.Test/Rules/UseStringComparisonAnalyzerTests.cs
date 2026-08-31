using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseStringComparisonAnalyzer,
    Meziantou.Analyzer.Rules.UseStringComparisonFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringComparisonAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add("MA0001");
        return test;
    }

    [Fact]
    public Task Equals_String_string_StringComparison_ShouldNotReportDiagnosticWhenStringComparisonIsSpecifiedAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = "test";
                    string.Equals(a, "v", System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IndexOf_String_StringComparison_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    "a".IndexOf("v", System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IndexOf_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|#0:"a".IndexOf("v")|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0074", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Use an overload of 'IndexOf' that has a StringComparison parameter"));
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    "a".IndexOf("v", System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StartsWith_String_StringComparison_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    "a".StartsWith("v", System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StartsWith_String_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|#0:"a".StartsWith("v")|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0074", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Use an overload of 'StartsWith' that has a StringComparison parameter"));
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    "a".StartsWith("v", System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Compare_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|#0:string.Compare("a", "v")|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0074", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Use an overload of 'Compare' that has a StringComparison parameter"));
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    string.Compare("a", "v", System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Compare_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    string.Compare("a", "v", ignoreCase: true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IndexOf_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    "".IndexOf("", 0, System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IndexOf_Char_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = "abc".IndexOf('a');
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IndexOf_Char_Int_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = "abc".IndexOf('a', 0);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LastIndexOf_Char_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = "abc".LastIndexOf('a');
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Contains_Char_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = "abc".Contains('a');
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Replace_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    "".Replace("", "");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludeWhenInAnExpressionContext()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq.Expressions;
            class TypeName
            {
                void WithSomething()
                {
                    _ = (Expression<Func<Something, bool>>)(s => s.SomeField.Contains(""));
                }

                public class Something
                {
                    public string SomeField { get; set; }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludeWhenInAnExpressionContext2()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq;
            using System.Linq.Expressions;
            class TypeName
            {
                void WithSomething()
                {
                    var op = new string[0];
                    _ = (Expression<Func<Something, bool>>)(s => op.ToList().Contains(s.SomeField));
                }

                public class Something
                {
                    public string SomeField { get; set; }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Contains_WithTrailingMessageParameter_ShouldInsertStringComparisonBeforeMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|MA0074:MyAssert.Contains("a", "b", "message")|};
                }
            }

            static class MyAssert
            {
                public static void Contains(string value, string substring, string message) { }
                public static void Contains(string value, string substring, System.StringComparison comparison, string message) { }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    MyAssert.Contains("a", "b", System.StringComparison.Ordinal, "message");
                }
            }

            static class MyAssert
            {
                public static void Contains(string value, string substring, string message) { }
                public static void Contains(string value, string substring, System.StringComparison comparison, string message) { }
            }
            """;

        return test.RunAsync();
    }
}

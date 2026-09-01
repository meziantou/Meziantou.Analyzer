using Microsoft.CodeAnalysis;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseStringComparisonAnalyzer,
    Meziantou.Analyzer.Rules.UseStringComparisonFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringComparisonAnalyzerTests
{
    [Fact]
    public Task Equals_String_string_StringComparison_ShouldNotReportDiagnosticWhenStringComparisonIsSpecifiedAsync()
    {
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
    public Task IndexOf_Char_ShouldNotReportCultureSensitiveDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = {|MA0001:"abc".IndexOf('a')|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = "abc".IndexOf('a', System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IndexOf_Char_Int_ShouldNotReportCultureSensitiveDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = {|MA0001:"abc".IndexOf('a', 0)|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = "abc".IndexOf('a', 0, System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LastIndexOf_Char_ShouldNotReportCultureSensitiveDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = {|MA0001:"abc".LastIndexOf('a')|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = "abc".LastIndexOf('a', System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Contains_Char_ShouldNotReportCultureSensitiveDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = {|MA0001:"abc".Contains('a')|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = "abc".Contains('a', System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Replace_ShouldNotReportCultureSensitiveDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|MA0001:"".Replace("", "")|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    "".Replace("", "", System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludeWhenInAnExpressionContext()
    {
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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

    [Fact]
    public Task Equals_String_string_ShouldReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|#0:System.String.Equals("a", "v")|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0001", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload of 'Equals' that has a StringComparison parameter"));
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    System.String.Equals("a", "v", System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_String_ShouldReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|#0:"a".Equals("v")|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0001", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload of 'Equals' that has a StringComparison parameter"));
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    "a".Equals("v", System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task String_GetHashCode_ShouldReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|#0:"a".GetHashCode()|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0001", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload of 'GetHashCode' that has a StringComparison parameter"));
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    "a".GetHashCode(System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IndexOf_Char_ShouldReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|#0:"a".IndexOf('v')|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0001", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload of 'IndexOf' that has a StringComparison parameter"));
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    "a".IndexOf('v', System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IndexOf_Char_Int_ShouldReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|#0:"abc".IndexOf('v', 0)|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0001", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload of 'IndexOf' that has a StringComparison parameter"));
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    "abc".IndexOf('v', 0, System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Contains_Char_ShouldReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|#0:"abc".Contains('a')|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0001", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload of 'Contains' that has a StringComparison parameter"));
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    "abc".Contains('a', System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Contains_Char_StringComparison_ShouldNotReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = "abc".Contains('a', System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LastIndexOf_Char_ShouldReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|#0:"abc".LastIndexOf('a')|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0001", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload of 'LastIndexOf' that has a StringComparison parameter"));

        return test.RunAsync();
    }

    [Fact]
    public Task JObject_Property_ShouldReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var obj = new Newtonsoft.Json.Linq.JObject();
                    {|MA0001:obj.Property("")|};
                }
            }

            namespace Newtonsoft.Json.Linq
            {
                public class JObject
                {
                    public void Property(string name) => throw null;
                    public void Property(string name, System.StringComparison comparison) => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MeziantouFrameworkAssertions_Assert_ShouldNotReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddMeziantouFrameworkAssertions();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    Meziantou.Framework.Assertions.Assert.Contains("abc", "abcdef");
                }
            }
            """;

        return test.RunAsync();
    }
}

using System.Security.Claims;
using System.Security.Principal;
using System.Text;
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
    public Task XunitAssert_ShouldNotReportCultureSensitiveDiagnostic()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddXunitV3();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|MA0001:Xunit.Assert.Contains("a", "abc")|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    Xunit.Assert.Contains("a", "abc", System.StringComparison.Ordinal);
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

    [Theory]
    [InlineData("MA0001", "s.Contains(c)", "s.Contains(c, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.Contains(r)", "s.Contains(r, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.StartsWith(c)", "s.StartsWith(c, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.StartsWith(r)", "s.StartsWith(r, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.EndsWith(c)", "s.EndsWith(c, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.EndsWith(r)", "s.EndsWith(r, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.IndexOf(c)", "s.IndexOf(c, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.IndexOf(r)", "s.IndexOf(r, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.IndexOf(c, 0)", "s.IndexOf(c, 0, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.IndexOf(r, 0)", "s.IndexOf(r, 0, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.IndexOf(c, 0, 1)", "s.IndexOf(c, 0, 1, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.IndexOf(r, 0, 1)", "s.IndexOf(r, 0, 1, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.LastIndexOf(c)", "s.LastIndexOf(c, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.LastIndexOf(r)", "s.LastIndexOf(r, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.LastIndexOf(c, 0)", "s.LastIndexOf(c, 0, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.LastIndexOf(r, 0)", "s.LastIndexOf(r, 0, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.LastIndexOf(c, 0, 1)", "s.LastIndexOf(c, 0, 1, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.LastIndexOf(r, 0, 1)", "s.LastIndexOf(r, 0, 1, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.Contains(a)", "s.Contains(a, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.Equals(a)", "s.Equals(a, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.Replace(a, b)", "s.Replace(a, b, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "s.GetHashCode()", "s.GetHashCode(System.StringComparison.Ordinal)")]
    [InlineData("MA0074", "s.StartsWith(a)", "s.StartsWith(a, System.StringComparison.Ordinal)")]
    [InlineData("MA0074", "s.EndsWith(a)", "s.EndsWith(a, System.StringComparison.Ordinal)")]
    [InlineData("MA0074", "s.IndexOf(a)", "s.IndexOf(a, System.StringComparison.Ordinal)")]
    [InlineData("MA0074", "s.IndexOf(a, 0)", "s.IndexOf(a, 0, System.StringComparison.Ordinal)")]
    [InlineData("MA0074", "s.IndexOf(a, 0, 1)", "s.IndexOf(a, 0, 1, System.StringComparison.Ordinal)")]
    [InlineData("MA0074", "s.LastIndexOf(a)", "s.LastIndexOf(a, System.StringComparison.Ordinal)")]
    [InlineData("MA0074", "s.LastIndexOf(a, 2)", "s.LastIndexOf(a, 2, System.StringComparison.Ordinal)")]
    [InlineData("MA0074", "s.LastIndexOf(a, 2, 1)", "s.LastIndexOf(a, 2, 1, System.StringComparison.Ordinal)")]
    [InlineData("MA0001", "string.Equals(a, b)", "string.Equals(a, b, System.StringComparison.Ordinal)")]
    [InlineData("MA0074", "string.Compare(a, b)", "string.Compare(a, b, System.StringComparison.Ordinal)")]
    [InlineData("MA0074", "string.Compare(a, 0, b, 0, 1)", "string.Compare(a, 0, b, 0, 1, System.StringComparison.Ordinal)")]
    public Task StringMethods_ReportDiagnosticDependingOnTheDefaultComparison(string ruleId, string code, string fixedCode)
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            class TypeName
            {
                public void Test(string s, string a, string b, char c, System.Text.Rune r)
                {
                    _ = {|{{ruleId}}:{{code}}|};
                }
            }
            """;
        test.FixedCode = $$"""
            class TypeName
            {
                public void Test(string s, string a, string b, char c, System.Text.Rune r)
                {
                    _ = {{fixedCode}};
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

    [Fact]
    public Task IndexOf_ReorderedNamedArguments_ShouldAddNamedArgument()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = {|MA0074:"abc".IndexOf(startIndex: 0, value: "a")|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = "abc".IndexOf(startIndex: 0, value: "a", comparisonType: System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IndexOf_NamedArgumentsInOrder_ShouldAddNamedArgument()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = {|MA0074:"abc".IndexOf(value: "a", startIndex: 0)|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = "abc".IndexOf(value: "a", startIndex: 0, comparisonType: System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringComparisonParameterIsNotLast_ShouldInsertArgumentAtParameterIndex()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|MA0074:Sample.Method("x", "y")|};
                }
            }

            static class Sample
            {
                public static void Method(string a, string b) => throw null;
                public static void Method(string a, System.StringComparison comparisonType, string b) => throw null;
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    Sample.Method("x", System.StringComparison.Ordinal, "y");
                }
            }

            static class Sample
            {
                public static void Method(string a, string b) => throw null;
                public static void Method(string a, System.StringComparison comparisonType, string b) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringComparisonParameterIsNotLast_ReorderedNamedArguments_ShouldAddNamedArgument()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    {|MA0074:Sample.Method(b: "y", a: "x")|};
                }
            }

            static class Sample
            {
                public static void Method(string a, string b) => throw null;
                public static void Method(string a, System.StringComparison comparisonType, string b) => throw null;
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    Sample.Method(b: "y", a: "x", comparisonType: System.StringComparison.Ordinal);
                }
            }

            static class Sample
            {
                public static void Method(string a, string b) => throw null;
                public static void Method(string a, System.StringComparison comparisonType, string b) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExtensionMethod_NamespaceNotImported_DisabledByDefault()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Test
            {
                public void A(Sample sample)
                {
                    sample.Find("a");
                }
            }

            public class Sample
            {
                public void Find(string value) => throw null;
            }

            namespace Ext
            {
                public static class SampleExtensions
                {
                    public static void Find(this Sample sample, string value, System.StringComparison comparison) => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExtensionMethod_NamespaceNotImported_EnabledForAnotherRule()
    {
        var test = new CodeFixTest();
        test.TestState.SetConfiguration("MA0001.include_extension_methods_from_not_imported_namespaces", "true");
        test.TestCode = """
            class Test
            {
                public void A(Sample sample)
                {
                    sample.Find("a");
                }
            }

            public class Sample
            {
                public void Find(string value) => throw null;
            }

            namespace Ext
            {
                public static class SampleExtensions
                {
                    public static void Find(this Sample sample, string value, System.StringComparison comparison) => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExtensionMethod_NamespaceNotImported_AddUsingDirective()
    {
        var test = new CodeFixTest();
        test.TestState.SetConfiguration("MA0074.include_extension_methods_from_not_imported_namespaces", "true");
        test.TestCode = """
            class Test
            {
                public void A(Sample sample)
                {
                    {|MA0074:sample.Find("a")|};
                }
            }

            public class Sample
            {
                public void Find(string value) => throw null;
            }

            namespace Ext
            {
                public static class SampleExtensions
                {
                    public static void Find(this Sample sample, string value, System.StringComparison comparison) => throw null;
                }
            }
            """;
        test.FixedCode = """
            using Ext;

            class Test
            {
                public void A(Sample sample)
                {
                    sample.Find("a", System.StringComparison.Ordinal);
                }
            }

            public class Sample
            {
                public void Find(string value) => throw null;
            }

            namespace Ext
            {
                public static class SampleExtensions
                {
                    public static void Find(this Sample sample, string value, System.StringComparison comparison) => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConditionalAccess_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Test
            {
                public void A(string value)
                {
                    _ = value?{|MA0074:.IndexOf("v")|};
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                public void A(string value)
                {
                    _ = value?.IndexOf("v", System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConditionalAccess_MemberAccessChain_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            class Test
            {
                public void A(Sample sample)
                {
                    _ = sample?{|MA0074:.Value.IndexOf("v")|}.ToString();
                }
            }

            public class Sample
            {
                public string Value { get; }
            }
            """;
        test.FixedCode = """
            class Test
            {
                public void A(Sample sample)
                {
                    _ = sample?.Value.IndexOf("v", System.StringComparison.Ordinal).ToString();
                }
            }

            public class Sample
            {
                public string Value { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConditionalAccess_ExtensionMethod_NamespaceNotImported_AddUsingDirective()
    {
        var test = new CodeFixTest();
        test.TestState.SetConfiguration("MA0074.include_extension_methods_from_not_imported_namespaces", "true");
        test.TestCode = """
            class Test
            {
                public void A(Sample sample)
                {
                    sample?{|MA0074:.Find("a")|};
                }
            }

            public class Sample
            {
                public void Find(string value) => throw null;
            }

            namespace Ext
            {
                public static class SampleExtensions
                {
                    public static void Find(this Sample sample, string value, System.StringComparison comparison) => throw null;
                }
            }
            """;
        test.FixedCode = """
            using Ext;

            class Test
            {
                public void A(Sample sample)
                {
                    sample?.Find("a", System.StringComparison.Ordinal);
                }
            }

            public class Sample
            {
                public void Find(string value) => throw null;
            }

            namespace Ext
            {
                public static class SampleExtensions
                {
                    public static void Find(this Sample sample, string value, System.StringComparison comparison) => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    // The methods of Microsoft.Extensions.Primitives are not checked, as the test project does not reference this package
    public static TheoryData<string> NonCultureSensitiveMethodDocumentationIds => new(UseStringComparisonAnalyzer.NonCultureSensitiveMethodDocumentationIds.Where(id => !id.StartsWith("M:Microsoft.Extensions.Primitives.", StringComparison.Ordinal)));

#pragma warning disable MA0001, MA0021, MA0074, CA1307, CA1309, CA1310, RS0030 // The methods are called without a StringComparison on purpose, and compared with a culture-sensitive comparison
    [Theory]
    [MemberData(nameof(NonCultureSensitiveMethodDocumentationIds))]
    public void NonCultureSensitiveMethod_DefaultComparisonIsOrdinal(string documentationId)
    {
        // Linguistic comparisons ignore the soft hyphen, so the results of these methods differ between an ordinal and a culture-sensitive comparison
        const char Ignorable = '\u00AD';
        const string IgnorableString = "\u00AD";
        const StringComparison Culture = StringComparison.InvariantCulture;
        const StringComparison Ordinal = StringComparison.Ordinal;

        switch (documentationId)
        {
            case "M:System.Char.Equals(System.Char)~System.Boolean":
                AssertOrdinal(Ignorable.Equals('\u200B'), ordinal: string.Equals(IgnorableString, "\u200B", Ordinal), culture: string.Equals(IgnorableString, "\u200B", Culture));
                break;

            case "M:System.IO.Path.GetRelativePath(System.String,System.String)~System.String":
                // The comparison is OrdinalIgnoreCase on Windows and macOS, and Ordinal on Linux
                AssertOrdinal(Path.GetRelativePath("/a/b" + IgnorableString, "/a/b/c"), ordinal: Path.Combine("..", "b", "c"), culture: "c");
                break;

            case "M:System.MemoryExtensions.EndsWith``1(System.ReadOnlySpan{``0},System.ReadOnlySpan{``0})~System.Boolean":
                AssertOrdinal(MemoryExtensions.EndsWith<char>("ab" + IgnorableString, "b"), ordinal: ("ab" + IgnorableString).EndsWith("b", Ordinal), culture: ("ab" + IgnorableString).EndsWith("b", Culture));
                break;

            case "M:System.MemoryExtensions.EndsWith``1(System.ReadOnlySpan{``0},``0)~System.Boolean":
                AssertOrdinal(MemoryExtensions.EndsWith<char>("ab", Ignorable), ordinal: "ab".EndsWith(IgnorableString, Ordinal), culture: "ab".EndsWith(IgnorableString, Culture));
                break;

            case "M:System.Security.Claims.ClaimsIdentity.#ctor(System.IO.BinaryReader)":
                {
                    using var stream = new MemoryStream();
                    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        CreateClaimsIdentity().WriteTo(writer);
                    }

                    stream.Position = 0;
                    using var reader = new BinaryReader(stream);
                    AssertOrdinalIgnoreCase(new ClaimsIdentity(reader));
                    break;
                }

            case "M:System.Security.Claims.ClaimsIdentity.#ctor(System.Security.Claims.ClaimsIdentity)":
                AssertOrdinalIgnoreCase(new CopiedClaimsIdentity(CreateClaimsIdentity()));
                break;

            case "M:System.Security.Claims.ClaimsIdentity.#ctor(System.Security.Principal.IIdentity,System.Collections.Generic.IEnumerable{System.Security.Claims.Claim},System.String,System.String,System.String)":
                AssertOrdinalIgnoreCase(CreateClaimsIdentity());
                break;

            case "M:System.String.Contains(System.Char)~System.Boolean":
                AssertOrdinal("abc".Contains(Ignorable), ordinal: "abc".Contains(IgnorableString, Ordinal), culture: "abc".Contains(IgnorableString, Culture));
                break;

            case "M:System.String.Contains(System.String)~System.Boolean":
                AssertOrdinal("abc".Contains(IgnorableString), ordinal: "abc".Contains(IgnorableString, Ordinal), culture: "abc".Contains(IgnorableString, Culture));
                break;

            case "M:System.String.EndsWith(System.Char)~System.Boolean":
                AssertOrdinal("abc".EndsWith(Ignorable), ordinal: "abc".EndsWith(IgnorableString, Ordinal), culture: "abc".EndsWith(IgnorableString, Culture));
                break;

            case "M:System.String.Equals(System.String)~System.Boolean":
                AssertOrdinal(("a" + IgnorableString).Equals("a"), ordinal: ("a" + IgnorableString).Equals("a", Ordinal), culture: ("a" + IgnorableString).Equals("a", Culture));
                break;

            case "M:System.String.Equals(System.String,System.String)~System.Boolean":
                AssertOrdinal(string.Equals("a" + IgnorableString, "a"), ordinal: string.Equals("a" + IgnorableString, "a", Ordinal), culture: string.Equals("a" + IgnorableString, "a", Culture));
                break;

            case "M:System.String.GetHashCode(System.ReadOnlySpan{System.Char})~System.Int32":
                AssertOrdinal(
                    string.GetHashCode(("a" + IgnorableString).AsSpan()) == string.GetHashCode("a".AsSpan()),
                    ordinal: string.GetHashCode(("a" + IgnorableString).AsSpan(), Ordinal) == string.GetHashCode("a".AsSpan(), Ordinal),
                    culture: string.GetHashCode(("a" + IgnorableString).AsSpan(), Culture) == string.GetHashCode("a".AsSpan(), Culture));
                break;

            case "M:System.String.GetHashCode~System.Int32":
                AssertOrdinal(
                    ("a" + IgnorableString).GetHashCode() == "a".GetHashCode(),
                    ordinal: ("a" + IgnorableString).GetHashCode(Ordinal) == "a".GetHashCode(Ordinal),
                    culture: ("a" + IgnorableString).GetHashCode(Culture) == "a".GetHashCode(Culture));
                break;

            case "M:System.String.IndexOf(System.Char)~System.Int32":
                AssertOrdinal("abc".IndexOf(Ignorable), ordinal: "abc".IndexOf(IgnorableString, Ordinal), culture: "abc".IndexOf(IgnorableString, Culture));
                break;

            case "M:System.String.IndexOf(System.Char,System.Int32)~System.Int32":
                AssertOrdinal("abc".IndexOf(Ignorable, 1), ordinal: "abc".IndexOf(IgnorableString, 1, Ordinal), culture: "abc".IndexOf(IgnorableString, 1, Culture));
                break;

            case "M:System.String.IndexOf(System.Char,System.Int32,System.Int32)~System.Int32":
                AssertOrdinal("abc".IndexOf(Ignorable, 1, 1), ordinal: "abc".IndexOf(IgnorableString, 1, 1, Ordinal), culture: "abc".IndexOf(IgnorableString, 1, 1, Culture));
                break;

            case "M:System.String.LastIndexOf(System.Char)~System.Int32":
                AssertOrdinal("abc".LastIndexOf(Ignorable), ordinal: "abc".LastIndexOf(IgnorableString, Ordinal), culture: "abc".LastIndexOf(IgnorableString, Culture));
                break;

            case "M:System.String.LastIndexOf(System.Char,System.Int32)~System.Int32":
                AssertOrdinal("abc".LastIndexOf(Ignorable, 1), ordinal: "abc".LastIndexOf(IgnorableString, 1, Ordinal), culture: "abc".LastIndexOf(IgnorableString, 1, Culture));
                break;

            case "M:System.String.LastIndexOf(System.Char,System.Int32,System.Int32)~System.Int32":
                AssertOrdinal("abc".LastIndexOf(Ignorable, 1, 1), ordinal: "abc".LastIndexOf(IgnorableString, 1, 1, Ordinal), culture: "abc".LastIndexOf(IgnorableString, 1, 1, Culture));
                break;

            case "M:System.String.Replace(System.String,System.String)~System.String":
                AssertOrdinal("abc".Replace("a" + IgnorableString + "b", "x"), ordinal: "abc".Replace("a" + IgnorableString + "b", "x", Ordinal), culture: "abc".Replace("a" + IgnorableString + "b", "x", Culture));
                break;

            case "M:System.String.StartsWith(System.Char)~System.Boolean":
                AssertOrdinal("abc".StartsWith(Ignorable), ordinal: "abc".StartsWith(IgnorableString, Ordinal), culture: "abc".StartsWith(IgnorableString, Culture));
                break;

            case "M:System.Text.Rune.Equals(System.Text.Rune)~System.Boolean":
                AssertOrdinal(new Rune(Ignorable).Equals(new Rune('\u200B')), ordinal: string.Equals(IgnorableString, "\u200B", Ordinal), culture: string.Equals(IgnorableString, "\u200B", Culture));
                break;

#if NET11_0_OR_GREATER
            case "M:System.String.Contains(System.Text.Rune)~System.Boolean":
                AssertOrdinal("abc".Contains(new Rune(Ignorable)), ordinal: "abc".Contains(IgnorableString, Ordinal), culture: "abc".Contains(IgnorableString, Culture));
                break;

            case "M:System.String.EndsWith(System.Text.Rune)~System.Boolean":
                AssertOrdinal("abc".EndsWith(new Rune(Ignorable)), ordinal: "abc".EndsWith(IgnorableString, Ordinal), culture: "abc".EndsWith(IgnorableString, Culture));
                break;

            case "M:System.String.IndexOf(System.Text.Rune)~System.Int32":
                AssertOrdinal("abc".IndexOf(new Rune(Ignorable)), ordinal: "abc".IndexOf(IgnorableString, Ordinal), culture: "abc".IndexOf(IgnorableString, Culture));
                break;

            case "M:System.String.IndexOf(System.Text.Rune,System.Int32)~System.Int32":
                AssertOrdinal("abc".IndexOf(new Rune(Ignorable), 1), ordinal: "abc".IndexOf(IgnorableString, 1, Ordinal), culture: "abc".IndexOf(IgnorableString, 1, Culture));
                break;

            case "M:System.String.IndexOf(System.Text.Rune,System.Int32,System.Int32)~System.Int32":
                AssertOrdinal("abc".IndexOf(new Rune(Ignorable), 1, 1), ordinal: "abc".IndexOf(IgnorableString, 1, 1, Ordinal), culture: "abc".IndexOf(IgnorableString, 1, 1, Culture));
                break;

            case "M:System.String.LastIndexOf(System.Text.Rune)~System.Int32":
                AssertOrdinal("abc".LastIndexOf(new Rune(Ignorable)), ordinal: "abc".LastIndexOf(IgnorableString, Ordinal), culture: "abc".LastIndexOf(IgnorableString, Culture));
                break;

            case "M:System.String.LastIndexOf(System.Text.Rune,System.Int32)~System.Int32":
                AssertOrdinal("abc".LastIndexOf(new Rune(Ignorable), 1), ordinal: "abc".LastIndexOf(IgnorableString, 1, Ordinal), culture: "abc".LastIndexOf(IgnorableString, 1, Culture));
                break;

            case "M:System.String.LastIndexOf(System.Text.Rune,System.Int32,System.Int32)~System.Int32":
                AssertOrdinal("abc".LastIndexOf(new Rune(Ignorable), 1, 1), ordinal: "abc".LastIndexOf(IgnorableString, 1, 1, Ordinal), culture: "abc".LastIndexOf(IgnorableString, 1, 1, Culture));
                break;

            case "M:System.String.StartsWith(System.Text.Rune)~System.Boolean":
                AssertOrdinal("abc".StartsWith(new Rune(Ignorable)), ordinal: "abc".StartsWith(IgnorableString, Ordinal), culture: "abc".StartsWith(IgnorableString, Culture));
                break;
#else
            case "M:System.String.Contains(System.Text.Rune)~System.Boolean":
            case "M:System.String.EndsWith(System.Text.Rune)~System.Boolean":
            case "M:System.String.IndexOf(System.Text.Rune)~System.Int32":
            case "M:System.String.IndexOf(System.Text.Rune,System.Int32)~System.Int32":
            case "M:System.String.IndexOf(System.Text.Rune,System.Int32,System.Int32)~System.Int32":
            case "M:System.String.LastIndexOf(System.Text.Rune)~System.Int32":
            case "M:System.String.LastIndexOf(System.Text.Rune,System.Int32)~System.Int32":
            case "M:System.String.LastIndexOf(System.Text.Rune,System.Int32,System.Int32)~System.Int32":
            case "M:System.String.StartsWith(System.Text.Rune)~System.Boolean":
                Xunit.Assert.Skip("The method is available from .NET 11");
                break;
#endif

            default:
                Assert.Fail($"No test checks the default comparison of '{documentationId}'");
                break;
        }

        static void AssertOrdinal<T>(T actual, T ordinal, T culture)
        {
            // Ensure the input is affected by the comparison, otherwise the test would always succeed
            Assert.NotEqual(culture, ordinal);
            Assert.Equal(ordinal, actual);
        }

        static ClaimsIdentity CreateClaimsIdentity() => new(new EmptyIdentity(), [new Claim("a", "1"), new Claim("b" + IgnorableString, "2")], "auth", "a", "role");

        static void AssertOrdinalIgnoreCase(ClaimsIdentity identity)
        {
            // OrdinalIgnoreCase: "A" matches "a", but "b" does not match "b\u00AD" as it would with a culture-sensitive comparison
            Assert.Equal("1", identity.FindFirst("A")?.Value);
            Assert.Null(identity.FindFirst("b"));
        }
    }
#pragma warning restore MA0001, MA0021, MA0074, CA1307, CA1309, CA1310, RS0030

    private sealed class EmptyIdentity : IIdentity
    {
        public string? AuthenticationType => null;
        public bool IsAuthenticated => false;
        public string? Name => null;
    }

#pragma warning disable CA1307 // The constructor is called without a StringComparison on purpose
    private sealed class CopiedClaimsIdentity(ClaimsIdentity other) : ClaimsIdentity(other);
#pragma warning restore CA1307
}

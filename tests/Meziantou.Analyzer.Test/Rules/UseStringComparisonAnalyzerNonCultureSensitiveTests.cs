using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseStringComparisonAnalyzer,
    Meziantou.Analyzer.Rules.UseStringComparisonFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringComparisonAnalyzerNonCultureSensitiveTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add("MA0074");
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
    public Task Equals_String_string_ShouldReportDiagnostic()
    {
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
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
    public Task IndexOf_Char_ShouldReportDiagnostic()
    {
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net100;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("Meziantou.Framework.Assertions", "2.0.1")]);
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

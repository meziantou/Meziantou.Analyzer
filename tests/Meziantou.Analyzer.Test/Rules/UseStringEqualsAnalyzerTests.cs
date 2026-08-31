using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseStringEqualsAnalyzer,
    Meziantou.Analyzer.Rules.UseStringEqualsFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringEqualsAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task Equals_StringLiteral_stringLiteral_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = {|#0:"a" == "v"|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0006", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Use string.Equals instead of Equals operator"));
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = string.Equals("a", "v", System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotEquals_StringLiteral_stringLiteral_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = {|#0:"a" != "v"|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0006", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Use string.Equals instead of NotEquals operator"));
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = !string.Equals("a", "v", System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_StringVariable_stringLiteral_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test(string str)
                {
                    var a = {|#0:str == "v"|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0006", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Use string.Equals instead of Equals operator"));
        test.FixedCode = """
            class TypeName
            {
                public void Test(string str)
                {
                    var a = string.Equals(str, "v", System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_ObjectVariable_stringLiteral_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    object str = "";
                    var a = str == "v";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_stringLiteral_null_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = "a" == null;
                    var b = null == "a";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_InIQueryableMethod_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TypeName
            {
                public void Test()
                {
                    IQueryable<string> query = null;
                    query = query.Where(i => i == "test");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_EmptyString_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = "" == "v";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_StringEmpty_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = string.Empty == "v";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Replace_Meziantou_Framework_EqualsOrdinal()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("Meziantou.Framework", "3.0.23")]);
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = {|MA0006:"a" == "b"|};
                }
            }
            """;
        test.CodeActionIndex = 2;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck;
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.FixedCode = """
            using Meziantou.Framework;

            class TypeName
            {
                public void Test()
                {
                    var a = "a".EqualsOrdinal("b");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Replace_Meziantou_Framework_EqualsIgnoreCase()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("Meziantou.Framework", "3.0.23")]);
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = {|MA0006:"a" == "b"|};
                }
            }
            """;
        test.CodeActionIndex = 3;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck;
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.FixedCode = """
            using Meziantou.Framework;

            class TypeName
            {
                public void Test()
                {
                    var a = "a".EqualsIgnoreCase("b");
                }
            }
            """;

        return test.RunAsync();
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseMultiLineXmlCommentSyntaxAnalyzer,
    Meziantou.Analyzer.Rules.UseMultiLineXmlCommentSyntaxFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseMultiLineXmlCommentSyntaxAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task SummarySingleLine_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0211:<summary>description</summary>|}
            class Sample { }
            """;
        test.FixedCode = """
            /// <summary>
            /// description
            /// </summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SingleLineSummaryWithNestedElement_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0211:<summary>This has <c>code</c> inside</summary>|}
            class Sample { }
            """;
        test.FixedCode = """
            /// <summary>
            /// This has <c>code</c> inside
            /// </summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SingleLineSummaryWithCData_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0211:<summary><![CDATA[Sample]]></summary>|}
            class Sample { }
            """;
        test.FixedCode = """
            /// <summary>
            /// <![CDATA[Sample]]>
            /// </summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FieldSummarySingleLine_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// {|MA0211:<summary>description</summary>|}
                public int Value;
            }
            """;
        test.FixedCode = """
            class Sample
            {
                /// <summary>
                /// description
                /// </summary>
                public int Value;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertySummarySingleLine_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// {|MA0211:<summary>description</summary>|}
                public int Value { get; set; }
            }
            """;
        test.FixedCode = """
            class Sample
            {
                /// <summary>
                /// description
                /// </summary>
                public int Value { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StructSummarySingleLine_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0211:<summary>description</summary>|}
            struct Sample { }
            """;
        test.FixedCode = """
            /// <summary>
            /// description
            /// </summary>
            struct Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RecordSummarySingleLine_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0211:<summary>description</summary>|}
            record Sample;
            """;
        test.FixedCode = """
            /// <summary>
            /// description
            /// </summary>
            record Sample;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyContent_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary></summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParamSingleLine_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// <param name="value">The value</param>
                public void Method(int value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SummaryContainingOnlyWhitespace_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary>   </summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultiLineDescription_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary>
            /// description
            /// </summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultiLineSummaryWithNestedElement_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary>
            /// This has <c>code</c> inside
            /// </summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultiLineSummaryWithCData_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary>
            /// <![CDATA[Sample]]>
            /// </summary>
            class Sample { }
            """;

        return test.RunAsync();
    }
}

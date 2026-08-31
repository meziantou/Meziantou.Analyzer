using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseInlineXmlCommentSyntaxWhenPossibleAnalyzer,
    Meziantou.Analyzer.Rules.UseInlineXmlCommentSyntaxWhenPossibleFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseInlineXmlCommentSyntaxWhenPossibleAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task SingleLineDescription_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0177:<summary>
            /// description
            /// </summary>|}
            class Sample { }
            """;
        test.FixedCode = """
            /// <summary>description</summary>
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
            /// description line 1
            /// description line 2
            /// </summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AlreadyInline_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary>description</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParamSingleLine_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// {|MA0177:<param name="value">
                /// The value
                /// </param>|}
                public void Method(int value) { }
            }
            """;
        test.FixedCode = """
            class Sample
            {
                /// <param name="value">The value</param>
                public void Method(int value) { }
            }
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
                /// {|MA0177:<summary>
                /// description
                /// </summary>|}
                public int Value;
            }
            """;
        test.FixedCode = """
            class Sample
            {
                /// <summary>description</summary>
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
                /// {|MA0177:<summary>
                /// description
                /// </summary>|}
                public int Value { get; set; }
            }
            """;
        test.FixedCode = """
            class Sample
            {
                /// <summary>description</summary>
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
            /// {|MA0177:<summary>
            /// description
            /// </summary>|}
            struct Sample { }
            """;
        test.FixedCode = """
            /// <summary>description</summary>
            struct Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RecordSummarySingleLine_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0177:<summary>
            /// description
            /// </summary>|}
            record Sample;
            """;
        test.FixedCode = """
            /// <summary>description</summary>
            record Sample;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RemarksSingleLine_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0177:<remarks>
            /// This is a remark
            /// </remarks>|}
            class Sample { }
            """;
        test.FixedCode = """
            /// <remarks>This is a remark</remarks>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReturnsSingleLine_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// {|MA0177:<returns>
                /// The result
                /// </returns>|}
                public int Method() => 42;
            }
            """;
        test.FixedCode = """
            class Sample
            {
                /// <returns>The result</returns>
                public int Method() => 42;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InnerXmlElements_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary>
            /// This has <c>
            /// code
            /// </c> inside
            /// </summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyContent_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0177:<summary>
            /// </summary>|}
            class Sample { }
            """;
        test.FixedCode = """
            /// <summary></summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TypeParamSingleLine_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0177:<typeparam name="T">
            /// The type parameter
            /// </typeparam>|}
            class Sample<T> { }
            """;
        test.FixedCode = """
            /// <typeparam name="T">The type parameter</typeparam>
            class Sample<T> { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExceptionSingleLine_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// {|MA0177:<exception cref="System.ArgumentNullException">
                /// Thrown when argument is null
                /// </exception>|}
                public void Method(string value) { }
            }
            """;
        test.FixedCode = """
            class Sample
            {
                /// <exception cref="System.ArgumentNullException">Thrown when argument is null</exception>
                public void Method(string value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValueSingleLine_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// {|MA0177:<value>
                /// The property value
                /// </value>|}
                public int Property { get; set; }
            }
            """;
        test.FixedCode = """
            class Sample
            {
                /// <value>The property value</value>
                public int Property { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ContentOnSameLineAsOpenTag_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0177:<summary>line 1
            /// </summary>|}
            class Sample { }
            """;
        test.FixedCode = """
            /// <summary>line 1</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ContentOnSameLineAsOpenTagAndCloseTag_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary>line 1
            /// line 2</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CDataSection_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary><![CDATA[Sample
            /// Text 123]]></summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EntityReference_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary>line1&#10;line2</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MaxLineLength_WouldExceedLimit_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("max_line_length", "50");
        test.TestCode = """
            /// <summary>
            /// This is a very long description that would exceed the max line length limit
            /// </summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MaxLineLength_WithinLimit_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("max_line_length", "100");
        test.TestCode = """
            /// {|MA0177:<summary>
            /// Short description
            /// </summary>|}
            class Sample { }
            """;
        test.FixedCode = """
            /// <summary>Short description</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MaxLineLength_NotConfigured_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0177:<summary>
            /// This is a very long description that could potentially exceed some line length limit
            /// </summary>|}
            class Sample { }
            """;
        test.FixedCode = """
            /// <summary>This is a very long description that could potentially exceed some line length limit</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }
}

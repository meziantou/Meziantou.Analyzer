using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.StringFormatShouldBeConstantAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class StringFormatShouldBeConstantAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task StringFormat_NoParameters_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = {|MA0183:string.Format("value without argument")|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithParameterButNoPlaceholder_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = {|MA0183:string.Format("value with argument", 123)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithParameterButEscapedBraces_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = {|MA0183:string.Format("value with argument {{0}}", 123)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithValidPlaceholder_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = string.Format("value with argument {0}", 123);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithNonConstantFormatString_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var format = "test {0}";
                    var result = string.Format(format, 123);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithConstantFormatStringFromLocal_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var format = "value without placeholder";
                    var result = {|MA0183:string.Format(format, 123)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithConstantFormatStringFromLocalAssignment_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    string format;
                    format = "value without placeholder";
                    var result = {|MA0183:string.Format(format, 123)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithReassignedFormatString_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method(string other)
                {
                    var format = "value without placeholder";
                    format = other;
                    var result = string.Format(format, 123);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithPrivateGetOnlyPropertyFormatString_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                private string Format { get; } = "value without placeholder";

                void Method()
                {
                    var result = {|MA0183:string.Format(Format, 123)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithPrivateGetOnlyPropertyFormatString_AssignedInConstructor_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                private string Format { get; } = "value without placeholder";

                public Test(string format)
                {
                    Format = format;
                }

                void Method()
                {
                    var result = string.Format(Format, 123);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithPrivateReadonlyFieldFormatString_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                private readonly string _format = "value without placeholder";

                void Method()
                {
                    var result = {|MA0183:string.Format(_format, 123)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithPrivateReadonlyFieldFormatString_AssignedInConstructor_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                private readonly string _format = "value without placeholder";

                public Test(string format)
                {
                    _format = format;
                }

                void Method()
                {
                    var result = string.Format(_format, 123);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithIFormatProvider_NoPlaceholder_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = {|MA0183:string.Format(System.Globalization.CultureInfo.InvariantCulture, "no placeholder", 123)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithIFormatProvider_WithPlaceholder_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = string.Format(System.Globalization.CultureInfo.InvariantCulture, "with placeholder {0}", 123);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithIFormatProvider_NoParameters_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = {|MA0183:string.Format(System.Globalization.CultureInfo.InvariantCulture, "no parameters")|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_MultiplePlaceholders_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = string.Format("value {0} and {1}", 123, 456);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_PlaceholderWithAlignment_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = string.Format("value {0,10}", 123);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_PlaceholderWithFormatSpecifier_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = string.Format("value {0:X}", 123);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_MixedEscapedAndValidBraces_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = string.Format("value {{escaped}} {0}", 123);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_OnlyEscapedBraces_NoParameters_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = {|MA0183:string.Format("value {{escaped}}", 123)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_MultipleParameters_NoPlaceholder_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = {|MA0183:string.Format("no placeholder", 123, 456, 789)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_EmptyString_NoParameters_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = {|MA0183:string.Format("")|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_EmptyString_WithParameters_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    var result = {|MA0183:string.Format("", 123)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_NonConstantFormat_NoArguments_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method(string format)
                {
                    var result = {|MA0183:string.Format(format)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("abc{{")]  // Valid: escaped opening brace
    [InlineData("abc}}")]  // Valid: escaped closing brace
    public Task StringFormat_ValidEscapedBraces_ShouldReportDiagnostic(string formatString)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;

            class Test
            {
                void Method()
                {
                    var result = {|MA0183:string.Format("{{formatString}}", 123)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("abc{")]    // Invalid: unclosed brace
    [InlineData("abc{0")]   // Invalid: unclosed placeholder
    [InlineData("abc{abc")] // Invalid: non-numeric placeholder without closing
    [InlineData("abc}")]    // Invalid: unmatched closing brace
    [InlineData("abc{a{")]  // Invalid: non-numeric with nested opening brace
    [InlineData("abc{0{")]  // Invalid: numeric with nested opening brace
    [InlineData("abc{0:")]  // Invalid: incomplete format specifier
    public Task StringFormat_MalformedFormatString_ShouldNotCrash(string formatString)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;

            class Test
            {
                void Method()
                {
                    var result = {|MA0183:string.Format("{{formatString}}", 123)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithIFormatProviderAndValidPlaceholder_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Globalization;

            class Test
            {
                void Method()
                {
                    var result = string.Format(CultureInfo.InvariantCulture, "{0}", CultureInfo.InvariantCulture);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithIFormatProviderAndNoPlaceholder_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Globalization;

            class Test
            {
                void Method()
                {
                    var result = {|MA0183:string.Format(CultureInfo.InvariantCulture, "", CultureInfo.InvariantCulture)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithUnicodeDigit_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    // Using Arabic-Indic digit ٠ (U+0660) instead of ASCII 0
                    var result = {|MA0183:string.Format("{٠}", 123)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithIFormatProviderAndMultiplePlaceholders_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Globalization;

            class Program
            {
                private static string DebuggerDisplay => string.Format(CultureInfo.InvariantCulture, "Column: {0}, Value: {1}, Invalid: {2}, Blank: {3}", "Column", "Value", true, false);

                static void Main(string[] args)
                {
                    Console.WriteLine(DebuggerDisplay);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithExplicitEmptyParamsArray_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Globalization;

            class Test
            {
                void Method()
                {
                    var result = {|MA0183:string.Format(CultureInfo.InvariantCulture, "no placeholders", new object[0])|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithMultiplePlaceholdersAndArguments_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            string.Format("Zero: {0}, One: {1}, Two: {2}, Four: {3}", "Answer is", 42, true, false);
            string.Format("{0} x {1} [{2} x {3}{4}{5}]", 0, 1, 2, 3, 4, args.Length);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConsoleWrite_NoArgs_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    Console.Write("NO PLACEHOLDERS");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConsoleWrite_WithArgButNoPlaceholder_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    {|MA0183:Console.Write("NO PLACEHOLDERS", true)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConsoleWrite_WithArgAndPlaceholder_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    Console.Write("Value: {0}", true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConsoleWrite_MultipleArgsNoPlaceholder_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    {|MA0183:Console.Write("NO PLACEHOLDERS", true, true)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConsoleWriteLine_NoArgs_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    Console.WriteLine("NO PLACEHOLDERS");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConsoleWriteLine_WithArgButNoPlaceholder_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    {|MA0183:Console.WriteLine("NO PLACEHOLDERS", true)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConsoleWriteLine_WithArgAndPlaceholder_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    Console.WriteLine("Value: {0}", true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConsoleWriteLine_MultipleArgsNoPlaceholder_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    {|MA0183:Console.WriteLine("NO PLACEHOLDERS", true, true)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConsoleWriteLine_ThreeArgsNoPlaceholder_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test
            {
                void Method()
                {
                    {|MA0183:Console.WriteLine("NO PLACEHOLDERS", true, true, true)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringBuilderAppendFormat_NoFormattingArgs_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;

            class Test
            {
                void Method()
                {
                    var sb = new StringBuilder();
                    {|MA0183:sb.AppendFormat("NO PLACEHOLDERS")|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringBuilderAppendFormat_WithArgButNoPlaceholder_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;

            class Test
            {
                void Method()
                {
                    var sb = new StringBuilder();
                    {|MA0183:sb.AppendFormat("NO PLACEHOLDERS", true)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringBuilderAppendFormat_WithArgAndPlaceholder_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;

            class Test
            {
                void Method()
                {
                    var sb = new StringBuilder();
                    sb.AppendFormat("Value: {0}", true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringBuilderAppendFormat_WithIFormatProviderAndNoPlaceholder_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Globalization;
            using System.Text;

            class Test
            {
                void Method()
                {
                    var sb = new StringBuilder();
                    {|MA0183:sb.AppendFormat(CultureInfo.InvariantCulture, "NO PLACEHOLDERS", true)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringBuilderAppendFormat_WithIFormatProviderAndPlaceholder_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Globalization;
            using System.Text;

            class Test
            {
                void Method()
                {
                    var sb = new StringBuilder();
                    sb.AppendFormat(CultureInfo.InvariantCulture, "Value: {0}", true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithParenthesizedArgAndPlaceholder_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Globalization;

            class Test
            {
                void Method(object obj)
                {
                    _ = string.Format(CultureInfo.InvariantCulture, "Format string with placeholder: '{0}'.", (obj is null ? string.Empty : "X"));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_WithParenthesizedArgAndNoPlaceholder_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Globalization;

            class Test
            {
                void Method(object obj)
                {
                    _ = {|MA0183:string.Format(CultureInfo.InvariantCulture, "Format string without placeholder.", (obj is null ? string.Empty : "X"))|};
                }
            }
            """;

        return test.RunAsync();
    }
}

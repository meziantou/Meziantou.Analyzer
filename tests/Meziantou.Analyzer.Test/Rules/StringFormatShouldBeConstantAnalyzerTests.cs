using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class StringFormatShouldBeConstantAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<StringFormatShouldBeConstantAnalyzer>();
    }

    [Fact]
    public async Task StringFormat_NoParameters_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = [|string.Format("value without argument")|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_WithParameterButNoPlaceholder_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = [|string.Format("value with argument", 123)|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_WithParameterButEscapedBraces_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = [|string.Format("value with argument {{0}}", 123)|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_WithValidPlaceholder_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = string.Format("value with argument {0}", 123);
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_WithNonConstantFormatString_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var format = "test {0}";
                        var result = string.Format(format, 123);
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_WithIFormatProvider_NoPlaceholder_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = [|string.Format(System.Globalization.CultureInfo.InvariantCulture, "no placeholder", 123)|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_WithIFormatProvider_WithPlaceholder_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = string.Format(System.Globalization.CultureInfo.InvariantCulture, "with placeholder {0}", 123);
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_WithIFormatProvider_NoParameters_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = [|string.Format(System.Globalization.CultureInfo.InvariantCulture, "no parameters")|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_MultiplePlaceholders_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = string.Format("value {0} and {1}", 123, 456);
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_PlaceholderWithAlignment_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = string.Format("value {0,10}", 123);
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_PlaceholderWithFormatSpecifier_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = string.Format("value {0:X}", 123);
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_MixedEscapedAndValidBraces_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = string.Format("value {{escaped}} {0}", 123);
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_OnlyEscapedBraces_NoParameters_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = [|string.Format("value {{escaped}}", 123)|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_MultipleParameters_NoPlaceholder_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = [|string.Format("no placeholder", 123, 456, 789)|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_EmptyString_NoParameters_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = [|string.Format("")|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_EmptyString_WithParameters_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = [|string.Format("", 123)|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_NonConstantFormat_NoArguments_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method(string format)
                    {
                        var result = [|string.Format(format)|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [InlineData("abc{{")]  // Valid: escaped opening brace
    [InlineData("abc}}")]  // Valid: escaped closing brace
    public async Task StringFormat_ValidEscapedBraces_ShouldReportDiagnostic(string formatString)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = [|string.Format("{{formatString}}", 123)|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [InlineData("abc{")]    // Invalid: unclosed brace
    [InlineData("abc{0")]   // Invalid: unclosed placeholder
    [InlineData("abc{abc")] // Invalid: non-numeric placeholder without closing
    [InlineData("abc}")]    // Invalid: unmatched closing brace
    [InlineData("abc{a{")]  // Invalid: non-numeric with nested opening brace
    [InlineData("abc{0{")]  // Invalid: numeric with nested opening brace
    [InlineData("abc{0:")]  // Invalid: incomplete format specifier
    public async Task StringFormat_MalformedFormatString_ShouldNotCrash(string formatString)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                using System;

                class Test
                {
                    void Method()
                    {
                        var result = [|string.Format("{{formatString}}", 123)|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_WithIFormatProviderAndValidPlaceholder_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;
                using System.Globalization;

                class Test
                {
                    void Method()
                    {
                        var result = string.Format(CultureInfo.InvariantCulture, "{0}", CultureInfo.InvariantCulture);
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_WithIFormatProviderAndNoPlaceholder_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;
                using System.Globalization;

                class Test
                {
                    void Method()
                    {
                        var result = [|string.Format(CultureInfo.InvariantCulture, "", CultureInfo.InvariantCulture)|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_WithUnicodeDigit_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Test
                {
                    void Method()
                    {
                        // Using Arabic-Indic digit ٠ (U+0660) instead of ASCII 0
                        var result = [|string.Format("{٠}", 123)|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_WithIFormatProviderAndMultiplePlaceholders_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_WithExplicitEmptyParamsArray_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;
                using System.Globalization;

                class Test
                {
                    void Method()
                    {
                        var result = [|string.Format(CultureInfo.InvariantCulture, "no placeholders", new object[0])|];
                    }
                }
                """)
            .ValidateAsync();
    }
}

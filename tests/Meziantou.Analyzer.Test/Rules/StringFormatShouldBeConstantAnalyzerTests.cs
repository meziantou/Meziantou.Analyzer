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
}

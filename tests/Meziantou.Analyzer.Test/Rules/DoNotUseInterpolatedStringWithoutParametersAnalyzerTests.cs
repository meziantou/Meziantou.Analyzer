using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.DoNotUseInterpolatedStringWithoutParametersAnalyzer,
    Meziantou.Analyzer.Rules.DoNotUseInterpolatedStringWithoutParametersFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseInterpolatedStringWithoutParametersAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task InterpolatedStringWithoutParameters_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var x = {|MA0184:$"Required attribute 'output' not found."|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RegularString_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var x = "Required attribute 'output' not found.";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithParameters_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var name = "output";
                    var x = $"Required attribute '{name}' not found.";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithoutParameters_AssignedToFormattableString_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class TypeName
            {
                public void Test()
                {
                    FormattableString x = $"Required attribute 'output' not found.";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithoutParameters_ConvertedToFormattableString_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class TypeName
            {
                public void Test(FormattableString fs)
                {
                }

                public void Run()
                {
                    Test($"Required attribute 'output' not found.");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithoutParameters_ReturnedAsIFormattable_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                public static System.IFormattable Run() => $"text";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithoutParameters_AssignedToIFormattable_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private System.IFormattable _value;

                public void Run()
                {
                    System.IFormattable local = $"text";
                    _value = $"text";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithoutParameters_ConvertedToIFormattable_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                public void Test(System.IFormattable value)
                {
                }

                public void Run()
                {
                    Test($"text");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithoutParameters_CastToIFormattable_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                public object Run() => (System.IFormattable)$"text";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithoutParameters_ConditionalConvertedToIFormattable_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                public System.IFormattable Run(bool condition) => condition ? $"a" : (System.IFormattable)$"b";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithoutParameters_ConvertedToObject_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                public object Run() => [|$"text"|];
            }
            """;
        test.FixedCode = """
            class Sample
            {
                public object Run() => "text";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithoutParameters_ArgumentOfMethodReturningFormattableString_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                public void Run()
                {
                    System.FormattableString value = Create([|$"text"|]);
                }

                private static System.FormattableString Create(string value) => throw null;
            }
            """;
        test.FixedCode = """
            class Sample
            {
                public void Run()
                {
                    System.FormattableString value = Create("text");
                }

                private static System.FormattableString Create(string value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithoutParameters_CustomInterpolatedStringHandler_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test(CustomInterpolatedStringHandler handler)
                {
                }

                public void Run()
                {
                    Test($"Required attribute 'output' not found.");
                }
            }

            [System.Runtime.CompilerServices.InterpolatedStringHandler]
            public struct CustomInterpolatedStringHandler
            {
                public CustomInterpolatedStringHandler(int literalLength, int formattedCount)
                {
                }

                public void AppendLiteral(string s)
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyInterpolatedString_CustomInterpolatedStringHandler_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test(CustomInterpolatedStringHandler handler)
                {
                }

                public void Run()
                {
                    Test($"");
                }
            }

            [System.Runtime.CompilerServices.InterpolatedStringHandler]
            public struct CustomInterpolatedStringHandler
            {
                public CustomInterpolatedStringHandler(int literalLength, int formattedCount)
                {
                }

                public void AppendLiteral(string s)
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithoutParameters_InReturnStatement_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public string Test()
                {
                    return {|MA0184:$"Required attribute 'output' not found."|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithoutParameters_InMethodArgument_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test(string message)
                {
                }

                public void Run()
                {
                    Test({|MA0184:$"Required attribute 'output' not found."|});
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithEmptyInterpolation_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var name = "test";
                    var x = $"Value: {name}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_ShouldConvertToRegularString()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var x = {|MA0184:$"Required attribute 'output' not found."|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    var x = "Required attribute 'output' not found.";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_ShouldHandleEscapedCharacters()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var x = {|MA0184:$"Line 1\nLine 2"|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    var x = "Line 1\nLine 2";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_ShouldUnescapeBraces()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public string Test() => {|MA0184:$"{{text}}"|};
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public string Test() => "{text}";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_Verbatim_ShouldUnescapeBracesAndQuotes()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public string Test() => {|MA0184:$@"{{""text"": ""\""}}"|};
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public string Test() => "{\"text\": \"\\\"}";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_EmptyString()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public string Test() => {|MA0184:$""|};
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public string Test() => "";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RawInterpolatedStringWithoutParameters_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """"
            class TypeName
            {
                public void Test()
                {
                    _ = {|MA0184:$"""
                        Sample
                        """|};
                }
            }
            """";
        test.FixedCode = """"
            class TypeName
            {
                public void Test()
                {
                    _ = """
                        Sample
                        """;
                }
            }
            """";

        return test.RunAsync();
    }

    [Fact]
    public Task RawInterpolatedStringWithMultipleDollarsAndBraces_ShouldRemoveAllDollars()
    {
        var test = CreateTest();
        test.MarkupOptions = MarkupOptions.TreatPositionIndicatorsAsCode;
        test.TestCode = """"
            class TypeName
            {
                public void Test()
                {
                    _ = {|MA0184:$$"""{unknown}"""|};
                }
            }
            """";
        test.FixedCode = """"
            class TypeName
            {
                public void Test()
                {
                    _ = """{unknown}""";
                }
            }
            """";

        return test.RunAsync();
    }

    [Fact]
    public Task MultiLineRawInterpolatedStringWithThreeDollarsAndBraces_ShouldRemoveAllDollars()
    {
        var test = CreateTest();
        test.MarkupOptions = MarkupOptions.TreatPositionIndicatorsAsCode;
        test.TestCode = """"
            class TypeName
            {
                public void Test()
                {
                    _ = {|MA0184:$$$"""
                        {{unknown}}
                        """|};
                }
            }
            """";
        test.FixedCode = """"
            class TypeName
            {
                public void Test()
                {
                    _ = """
                        {{unknown}}
                        """;
                }
            }
            """";

        return test.RunAsync();
    }

    [Fact]
    public Task RawInterpolatedStringWithDollarInLeadingTrivia_ShouldPreserveTrivia()
    {
        var test = CreateTest();
        test.MarkupOptions = MarkupOptions.TreatPositionIndicatorsAsCode;
        test.TestCode = """"
            class TypeName
            {
                public void Test()
                {
                    _ = /* $ */ {|MA0184:$$"""{unknown}"""|};
                }
            }
            """";
        test.FixedCode = """"
            class TypeName
            {
                public void Test()
                {
                    _ = /* $ */ """{unknown}""";
                }
            }
            """";

        return test.RunAsync();
    }
}

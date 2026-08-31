using Microsoft.CodeAnalysis;
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
}

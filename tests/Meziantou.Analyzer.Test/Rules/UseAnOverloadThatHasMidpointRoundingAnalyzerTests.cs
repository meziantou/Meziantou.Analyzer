using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseAnOverloadThatHasMidpointRoundingAnalyzer,
    Meziantou.Analyzer.Rules.UseAnOverloadThatHasMidpointRoundingFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseAnOverloadThatHasMidpointRoundingAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task MathRoundWithoutMode_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    _ = [|System.Math.Round(2.5)|];
                    _ = [|System.Math.Round(2.5, 1)|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MathRoundWithMode_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    _ = System.Math.Round(2.5, System.MidpointRounding.AwayFromZero);
                    _ = System.Math.Round(2.5, 1, System.MidpointRounding.AwayFromZero);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(0, "ToEven")]
    [InlineData(1, "AwayFromZero")]
    [InlineData(2, "ToZero")]
    [InlineData(3, "ToNegativeInfinity")]
    [InlineData(4, "ToPositiveInfinity")]
    public Task MathRound_CodeFix_SuggestsEachMidpointRoundingValue(int codeFixIndex, string midpointRoundingMember)
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    _ = [|System.Math.Round(2.5)|];
                }
            }
            """;
        test.CodeActionIndex = codeFixIndex;
        test.FixedCode = $$"""
            class Test
            {
                void A()
                {
                    _ = System.Math.Round(2.5, System.MidpointRounding.{{midpointRoundingMember}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MathFRoundWithoutMode_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    _ = [|System.MathF.Round(2.5f)|];
                    _ = [|System.MathF.Round(2.5f, 1)|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MathFRoundWithMode_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    _ = System.MathF.Round(2.5f, System.MidpointRounding.AwayFromZero);
                    _ = System.MathF.Round(2.5f, 1, System.MidpointRounding.AwayFromZero);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DecimalRoundWithoutMode_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(decimal value)
                {
                    _ = [|decimal.Round(value)|];
                    _ = [|decimal.Round(value, 1)|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DecimalRoundWithMode_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(decimal value)
                {
                    _ = decimal.Round(value, System.MidpointRounding.AwayFromZero);
                    _ = decimal.Round(value, 1, System.MidpointRounding.AwayFromZero);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(0, "ToEven")]
    [InlineData(1, "AwayFromZero")]
    [InlineData(2, "ToZero")]
    [InlineData(3, "ToNegativeInfinity")]
    [InlineData(4, "ToPositiveInfinity")]
    public Task DecimalRound_CodeFix_SuggestsEachMidpointRoundingValue(int codeFixIndex, string midpointRoundingMember)
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(decimal value)
                {
                    _ = [|decimal.Round(value, 1)|];
                }
            }
            """;
        test.CodeActionIndex = codeFixIndex;
        test.FixedCode = $$"""
            class Test
            {
                void A(decimal value)
                {
                    _ = decimal.Round(value, 1, System.MidpointRounding.{{midpointRoundingMember}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FloatingPointImplementationsRoundWithoutMode_ReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = """
            class Test
            {
                void A(double d, float f, System.Half h)
                {
                    _ = [|double.Round(d)|];
                    _ = [|double.Round(d, 1)|];
                    _ = [|float.Round(f)|];
                    _ = [|float.Round(f, 1)|];
                    _ = [|System.Half.Round(h)|];
                    _ = [|System.Half.Round(h, 1)|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FloatingPointImplementationsRoundWithMode_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = """
            class Test
            {
                void A(double d, float f, System.Half h)
                {
                    _ = double.Round(d, System.MidpointRounding.AwayFromZero);
                    _ = double.Round(d, 1, System.MidpointRounding.AwayFromZero);
                    _ = float.Round(f, System.MidpointRounding.AwayFromZero);
                    _ = float.Round(f, 1, System.MidpointRounding.AwayFromZero);
                    _ = System.Half.Round(h, System.MidpointRounding.AwayFromZero);
                    _ = System.Half.Round(h, 1, System.MidpointRounding.AwayFromZero);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IFloatingPointRoundWithoutMode_ReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = """
            using System.Numerics;

            class Test
            {
                static T Round<T>(T value) where T : IFloatingPoint<T>
                {
                    _ = [|T.Round(value)|];
                    return [|T.Round(value, 1)|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IFloatingPointRoundWithMode_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = """
            using System.Numerics;

            class Test
            {
                static T Round<T>(T value) where T : IFloatingPoint<T>
                {
                    _ = T.Round(value, System.MidpointRounding.AwayFromZero);
                    return T.Round(value, 1, System.MidpointRounding.AwayFromZero);
                }
            }
            """;

        return test.RunAsync();
    }
}

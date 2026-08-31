using Microsoft.CodeAnalysis;
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
                    _ = {|MA0193:System.Math.Round(2.5)|};
                    _ = {|MA0193:System.Math.Round(2.5, 1)|};
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
                    _ = {|MA0193:System.Math.Round(2.5)|};
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
                    _ = {|MA0193:System.MathF.Round(2.5f)|};
                    _ = {|MA0193:System.MathF.Round(2.5f, 1)|};
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
                    _ = {|MA0193:decimal.Round(value)|};
                    _ = {|MA0193:decimal.Round(value, 1)|};
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
                    _ = {|MA0193:decimal.Round(value, 1)|};
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
        test.TestCode = """
            class Test
            {
                void A(double d, float f, System.Half h)
                {
                    _ = {|MA0193:double.Round(d)|};
                    _ = {|MA0193:double.Round(d, 1)|};
                    _ = {|MA0193:float.Round(f)|};
                    _ = {|MA0193:float.Round(f, 1)|};
                    _ = {|MA0193:System.Half.Round(h)|};
                    _ = {|MA0193:System.Half.Round(h, 1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FloatingPointImplementationsRoundWithMode_NoDiagnostic()
    {
        var test = CreateTest();
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
        test.TestCode = """
            using System.Numerics;

            class Test
            {
                static T Round<T>(T value) where T : IFloatingPoint<T>
                {
                    _ = {|MA0193:T.Round(value)|};
                    return {|MA0193:T.Round(value, 1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IFloatingPointRoundWithMode_NoDiagnostic()
    {
        var test = CreateTest();
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

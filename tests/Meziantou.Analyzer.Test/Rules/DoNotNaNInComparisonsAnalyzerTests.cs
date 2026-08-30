using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.DoNotNaNInComparisonsAnalyzer,
    Meziantou.Analyzer.Rules.DoNotNaNInComparisonsFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotNaNInComparisonsAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task Comparisons()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    _ = 1d == 0d;
                    _ = 1d != 0d;
                    _ = 0d == [|double.NaN|];
                    _ = 0d != [|double.NaN|];
                    _ = [|double.NaN|] == 0d;
                    _ = [|double.NaN|] != 0d;

                    _ = 1f == 0f;
                    _ = 1f != 0f;
                    _ = 0f == [|float.NaN|];
                    _ = 0f != [|float.NaN|];
                    _ = [|float.NaN|] == 0f;
                    _ = [|float.NaN|] != 0f;

                    _ = (double)[|float.NaN|] != 1f;

                    System.Half halfValue = (System.Half)0;
                    _ = halfValue == [|System.Half.NaN|];
                    _ = halfValue != [|System.Half.NaN|];
                    _ = [|System.Half.NaN|] == halfValue;
                    _ = [|System.Half.NaN|] != halfValue;

                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Comparisons_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(double value)
                {
                    _ = value == [|double.NaN|];
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A(double value)
                {
                    _ = double.IsNaN(value);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Comparisons_CodeFix_Float()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(float value)
                {
                    _ = value != [|float.NaN|];
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A(float value)
                {
                    _ = !float.IsNaN(value);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Comparisons_CodeFix_Half()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                void A(Half value)
                {
                    _ = value == [|Half.NaN|];
                }
            }
            """;
        test.FixedCode = """
            using System;
            class Test
            {
                void A(Half value)
                {
                    _ = Half.IsNaN(value);
                }
            }
            """;

        return test.RunAsync();
    }
}

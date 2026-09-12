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
                    _ = 0d == {|MA0082:double.NaN|};
                    _ = 0d != {|MA0082:double.NaN|};
                    _ = {|MA0082:double.NaN|} == 0d;
                    _ = {|MA0082:double.NaN|} != 0d;
                    _ = 0d < {|MA0082:double.NaN|};
                    _ = 0d <= {|MA0082:double.NaN|};
                    _ = 0d > {|MA0082:double.NaN|};
                    _ = 0d >= {|MA0082:double.NaN|};
                    _ = {|MA0082:double.NaN|} < 0d;

                    _ = 1f == 0f;
                    _ = 1f != 0f;
                    _ = 0f == {|MA0082:float.NaN|};
                    _ = 0f != {|MA0082:float.NaN|};
                    _ = {|MA0082:float.NaN|} == 0f;
                    _ = {|MA0082:float.NaN|} != 0f;

                    _ = (double){|MA0082:float.NaN|} != 1f;

                    System.Half halfValue = (System.Half)0;
                    _ = halfValue == {|MA0082:System.Half.NaN|};
                    _ = halfValue != {|MA0082:System.Half.NaN|};
                    _ = {|MA0082:System.Half.NaN|} == halfValue;
                    _ = {|MA0082:System.Half.NaN|} != halfValue;

                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Comparisons_GenericMath_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Numerics;
            class Test
            {
                void A<T>(T value) where T : IFloatingPointIeee754<T>
                {
                    _ = value == {|MA0082:T.NaN|};
                }

                void B<T>(T value) where T : IBinaryFloatingPointIeee754<T>
                {
                    _ = {|MA0082:T.NaN|} != value;
                }
            }
            """;
        test.FixedCode = """
            using System.Numerics;
            class Test
            {
                void A<T>(T value) where T : IFloatingPointIeee754<T>
                {
                    _ = T.IsNaN(value);
                }

                void B<T>(T value) where T : IBinaryFloatingPointIeee754<T>
                {
                    _ = !T.IsNaN(value);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Comparisons_GenericMath_BothOperandsAreNaN_NoCodeFix()
    {
        const string Source = """
            using System.Numerics;
            class Test
            {
                void A<T>() where T : IFloatingPointIeee754<T>
                {
                    _ = {|MA0082:T.NaN|} == {|MA0082:T.NaN|};
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = Source;
        test.FixedCode = Source;

        return test.RunAsync();
    }

    [Fact]
    public Task Comparisons_RelationalOperators_NoCodeFix()
    {
        const string Source = """
            using System;
            class Test
            {
                void A(double doubleValue, float floatValue, Half halfValue)
                {
                    _ = doubleValue < {|MA0082:double.NaN|};
                    _ = doubleValue <= {|MA0082:double.NaN|};
                    _ = doubleValue > {|MA0082:double.NaN|};
                    _ = doubleValue >= {|MA0082:double.NaN|};
                    _ = {|MA0082:float.NaN|} < floatValue;
                    _ = halfValue >= {|MA0082:Half.NaN|};
                }

                void B<T>(T value) where T : System.Numerics.IFloatingPointIeee754<T>
                {
                    _ = value < {|MA0082:T.NaN|};
                    _ = value >= {|MA0082:T.NaN|};
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = Source;
        test.FixedCode = Source;

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
                    _ = value == {|MA0082:double.NaN|};
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
                    _ = value != {|MA0082:float.NaN|};
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
    public Task Comparisons_BothOperandsAreNaN_NoCodeFix()
    {
        const string Source = """
            class Test
            {
                void A()
                {
                    _ = {|MA0082:double.NaN|} == {|MA0082:double.NaN|};
                    _ = {|MA0082:double.NaN|} != {|MA0082:double.NaN|};
                    _ = (double){|MA0082:float.NaN|} == {|MA0082:double.NaN|};
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = Source;
        test.FixedCode = Source;

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
                    _ = value == {|MA0082:Half.NaN|};
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.NonFlagsEnumsShouldNotBeMarkedWithFlagsAttributeAnalyzer,
    Meziantou.Analyzer.Rules.NonFlagsEnumsShouldNotBeMarkedWithFlagsAttributeFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class NonFlagsEnumsShouldNotBeMarkedWithFlagsAttributeAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task NonPowerOfTwo()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum [|Test|] : byte
            {
                A = 1,
                B = 2,
                C = 5, // Non valid
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonPowerOfTwo_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum [|Test|] : byte
            {
                A = 1,
                B = 2,
                C = 5,
            }
            """;
        test.FixedCode = """
            enum Test : byte
            {
                A = 1,
                B = 2,
                C = 5,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PowerOfTwoOrCombination()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum Test : byte
            {
                A = 1,
                B = 2,
                C = 3,
                D = 4,
                E = D | A,
                F = 8,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PowerOfTwoOrCombinationUsingHexa()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum Test
            {
                A = 0x0,
                B = 0x1,
                C = 0x2,
                D = 0x4,
                E = 0x8,
                F = 0x10,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PowerOfTwo_NegativeValue()
    {
        var options = "";
        for (var i = 0; i < 32; i++)
        {
            options += $"    Option{(i + 1).ToString("00", CultureInfo.InvariantCulture)} = unchecked((int)0b_{Convert.ToString(1 << i, toBase: 2).PadLeft(32, '0')}),\n";
        }

        var test = CreateTest();
        test.TestCode = $$"""
            [System.Flags]
            enum Test
            {
                None     = 0b_00000000000000000000000000000000,
            {{options}}
                All      = ~None,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PowerOfTwo_NegativeValue_Sbyte()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum Test : sbyte
            {
                None     = 0,
                Option01 = 1,
                Option02 = 2,
                Option03 = 4,
                Option04 = 8,
                Option05 = 16,
                Option06 = 32,
                Option07 = 64,
                Option08 = -128,
                All      = ~None,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AllBitSet_WithoutConfiguration()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum [|Test|]
            {
                None     = 0,
                Option1  = 1,
                All      = ~None,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AllBitSet_WithConfiguration()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0062.allow_all_bits_set_value", "true");
        test.TestCode = """
            [System.Flags]
            enum Test
            {
                None     = 0,
                Option1  = 1,
                All      = ~None,
            }
            """;

        return test.RunAsync();
    }
}

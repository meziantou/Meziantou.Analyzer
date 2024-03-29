﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class NonFlagsEnumsShouldNotBeMarkedWithFlagsAttributeAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<NonFlagsEnumsShouldNotBeMarkedWithFlagsAttributeAnalyzer>();
    }

    [Fact]
    public async Task NonPowerOfTwo()
    {
        const string SourceCode = @"
[System.Flags]
enum [||]Test : byte
{
    A = 1,
    B = 2,
    C = 5, // Non valid
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PowerOfTwoOrCombination()
    {
        const string SourceCode = @"
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
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PowerOfTwoOrCombinationUsingHexa()
    {
        const string SourceCode = @"
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
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PowerOfTwo_NegativeValue()
    {
        var options = "";
        for (var i = 0; i < 32; i++)
        {
            options += $"    Option{(i + 1).ToString("00", CultureInfo.InvariantCulture)} = unchecked((int)0b_{Convert.ToString(1 << i, toBase: 2).PadLeft(32, '0')}),\n";
        }

        var sourceCode = $$"""
[System.Flags]
enum Test
{
    None     = 0b_00000000000000000000000000000000,
{{options}}
    All      = ~None,
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PowerOfTwo_NegativeValue_Sbyte()
    {
        var sourceCode = $$"""
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
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AllBitSet_WithoutConfiguration()
    {
        var sourceCode = $$"""
[System.Flags]
enum [||]Test
{
    None     = 0,
    Option1  = 1,
    All      = ~None,
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AllBitSet_WithConfiguration()
    {
        var sourceCode = """
[System.Flags]
enum Test
{
    None     = 0,
    Option1  = 1,
    All      = ~None,
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .AddAnalyzerConfiguration("MA0062.allow_all_bits_set_value", "true")
              .ValidateAsync();
    }
}

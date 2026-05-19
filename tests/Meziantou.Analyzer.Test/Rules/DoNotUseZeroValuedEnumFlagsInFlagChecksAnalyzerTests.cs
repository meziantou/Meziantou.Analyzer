using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseZeroValuedEnumFlagsInFlagChecksAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotUseZeroValuedEnumFlagsInFlagChecksAnalyzer>();
    }

    [Fact]
    public async Task EqualityCheck_ZeroFlag_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                }

                class Sample
                {
                    bool M(MyEnum value) => [|(value & MyEnum.None) == MyEnum.None|];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task EqualityCheck_ZeroLiteral_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                }

                class Sample
                {
                    bool M(MyEnum value) => [|(value & MyEnum.None) == 0|];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NotEqualsCheck_ZeroFlag_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                }

                class Sample
                {
                    bool M(MyEnum value) => [|(value & MyEnum.None) != MyEnum.None|];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task IsPatternCheck_ZeroFlag_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                }

                class Sample
                {
                    bool M(MyEnum value) => [|(value & MyEnum.None) is MyEnum.None|];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task IsNotPatternCheck_ZeroFlag_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                }

                class Sample
                {
                    bool M(MyEnum value) => [|(value & MyEnum.None) is not MyEnum.None|];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task HasFlag_ZeroFlag_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                }

                class Sample
                {
                    bool M(MyEnum value) => [|value.HasFlag(MyEnum.None)|];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task HasFlag_ExplicitZeroCast_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                }

                class Sample
                {
                    bool M(MyEnum value) => [|value.HasFlag((MyEnum)0)|];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task HasFlagsExtension_ZeroFlag_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                }

                static class MyEnumExtensions
                {
                    public static bool HasFlags(this MyEnum value, MyEnum flags) => (value & flags) == flags;
                }

                class Sample
                {
                    bool M(MyEnum value) => [|value.HasFlags(MyEnum.None)|];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NonZeroFlag_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                }

                class Sample
                {
                    bool M(MyEnum value) => (value & MyEnum.Flag1) == MyEnum.Flag1;
                    bool M2(MyEnum value) => value.HasFlag(MyEnum.Flag1);
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task IntegerBitwiseCheck_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    bool M(int value) => (value & 1) == 1;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public void Rule_SeverityAndDefault()
    {
        var rule = new DoNotUseZeroValuedEnumFlagsInFlagChecksAnalyzer().SupportedDiagnostics[0];
        Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
        Assert.True(rule.IsEnabledByDefault);
    }
}

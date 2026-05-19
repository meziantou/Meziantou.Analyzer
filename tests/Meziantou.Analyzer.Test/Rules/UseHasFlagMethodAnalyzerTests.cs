using System.Linq;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseHasFlagMethodAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseHasFlagMethodAnalyzer>()
            .WithCodeFixProvider<UseHasFlagMethodFixer>();
    }

    [Fact]
    public async Task EqualityCheck_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) == MyEnum.Flag1|};
                }
                """)
            .ShouldFixCodeWith("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => value.HasFlag(MyEnum.Flag1);
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task EqualityCheck_ReversedAndOperands_ReportDiagnostic()
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
                    bool M(MyEnum value) => {|MA0192:(MyEnum.Flag1 & value) == MyEnum.Flag1|};
                }
                """)
            .ShouldFixCodeWith("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                }

                class Sample
                {
                    bool M(MyEnum value) => value.HasFlag(MyEnum.Flag1);
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task IsPatternCheck_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) is MyEnum.Flag1|};
                }
                """)
            .ShouldFixCodeWith("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => value.HasFlag(MyEnum.Flag1);
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NotEqualsCheck_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) != MyEnum.Flag1|};
                }
                """)
            .ShouldFixCodeWith("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => !value.HasFlag(MyEnum.Flag1);
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task IsNotPatternCheck_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) is not MyEnum.Flag1|};
                }
                """)
            .ShouldFixCodeWith("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => !value.HasFlag(MyEnum.Flag1);
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task EqualsZeroCheck_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) == 0|};
                }
                """)
            .ShouldFixCodeWith("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => !value.HasFlag(MyEnum.Flag1);
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NotEqualsZeroCheck_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) != 0|};
                }
                """)
            .ShouldFixCodeWith("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => value.HasFlag(MyEnum.Flag1);
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task IsPatternZeroCheck_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) is 0|};
                }
                """)
            .ShouldFixCodeWith("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => !value.HasFlag(MyEnum.Flag1);
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task IsNotPatternZeroCheck_ReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) is not 0|};
                }
                """)
            .ShouldFixCodeWith("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => value.HasFlag(MyEnum.Flag1);
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ZeroFlagEqualityCheck_ReportDiagnostic()
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
                    bool M(MyEnum value) => {|MA0201:(value & MyEnum.None) == MyEnum.None|};
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ZeroLiteralEqualityCheck_ReportDiagnostic()
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
                    bool M(MyEnum value) => {|MA0201:(value & MyEnum.None) == 0|};
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ZeroFlagNotEqualsCheck_ReportDiagnostic()
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
                    bool M(MyEnum value) => {|MA0201:(value & MyEnum.None) != MyEnum.None|};
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ZeroFlagIsPatternCheck_ReportDiagnostic()
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
                    bool M(MyEnum value) => {|MA0201:(value & MyEnum.None) is MyEnum.None|};
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ZeroFlagIsNotPatternCheck_ReportDiagnostic()
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
                    bool M(MyEnum value) => {|MA0201:(value & MyEnum.None) is not MyEnum.None|};
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task HasFlagZeroFlag_ReportDiagnostic()
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
                    bool M(MyEnum value) => {|MA0201:value.HasFlag(MyEnum.None)|};
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task HasFlagExplicitZeroCast_ReportDiagnostic()
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
                    bool M(MyEnum value) => {|MA0201:value.HasFlag((MyEnum)0)|};
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task DifferentFlag_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => (value & MyEnum.Flag1) == MyEnum.Flag2;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task HasFlagsExtensionZeroFlag_NoDiagnostic()
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
                    bool M(MyEnum value) => value.HasFlags(MyEnum.None);
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NonZeroHasFlag_NoDiagnostic()
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
                    bool M(MyEnum value) => value.HasFlag(MyEnum.Flag1);
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task CombinedFlag_NotEqualsZero_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                    Flag1AndFlag2 = Flag1 | Flag2,
                }

                class Sample
                {
                    bool M(MyEnum value) => (value & MyEnum.Flag1AndFlag2) != 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task DifferentFlag_IsNotPattern_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                [System.Flags]
                enum MyEnum
                {
                    None = 0,
                    Flag1 = 1,
                    Flag2 = 2,
                }

                class Sample
                {
                    bool M(MyEnum value) => (value & MyEnum.Flag1) is not MyEnum.Flag2;
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
    public async Task NullableEnum_NoDiagnostic()
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
                    bool M(MyEnum? value) => (value & MyEnum.Flag1) == MyEnum.Flag1;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public void MA0192_SeverityAndDefault()
    {
        var rule = new UseHasFlagMethodAnalyzer().SupportedDiagnostics.Single(r => r.Id == RuleIdentifiers.UseHasFlagMethod);
        Assert.Equal(DiagnosticSeverity.Info, rule.DefaultSeverity);
        Assert.False(rule.IsEnabledByDefault);
    }

    [Fact]
    public void MA0201_SeverityAndDefault()
    {
        var rule = new UseHasFlagMethodAnalyzer().SupportedDiagnostics.Single(r => r.Id == RuleIdentifiers.DoNotUseZeroValuedEnumFlagsInFlagChecks);
        Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
        Assert.True(rule.IsEnabledByDefault);
    }
}

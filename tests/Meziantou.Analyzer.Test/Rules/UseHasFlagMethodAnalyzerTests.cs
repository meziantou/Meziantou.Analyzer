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
                    bool M(MyEnum value) => [|(value & MyEnum.Flag1) == MyEnum.Flag1|];
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
                    bool M(MyEnum value) => [|(MyEnum.Flag1 & value) == MyEnum.Flag1|];
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
                    bool M(MyEnum value) => [|(value & MyEnum.Flag1) is MyEnum.Flag1|];
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
                    bool M(MyEnum value) => [|(value & MyEnum.Flag1) != MyEnum.Flag1|];
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
                    bool M(MyEnum value) => [|(value & MyEnum.Flag1) is not MyEnum.Flag1|];
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
    public void Rule_SeverityAndDefault()
    {
        var rule = new UseHasFlagMethodAnalyzer().SupportedDiagnostics[0];
        Assert.Equal(DiagnosticSeverity.Info, rule.DefaultSeverity);
        Assert.False(rule.IsEnabledByDefault);
    }
}

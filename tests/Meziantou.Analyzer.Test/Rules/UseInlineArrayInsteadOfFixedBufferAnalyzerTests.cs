#if CSHARP12_OR_GREATER
using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseInlineArrayInsteadOfFixedBufferAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseInlineArrayInsteadOfFixedBufferAnalyzer>()
            .WithCodeFixProvider<UseInlineArrayInsteadOfFixedBufferFixer>()
            .WithTargetFramework(TargetFramework.Net10_0);
    }

    [Fact]
    public async Task FixedByteBuffer_InlineArray16Exists_UseCodeFix()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                unsafe struct IpAddressBuffer
                {
                    public fixed byte [||]IpAddress[16];
                }
                """)
            .ShouldFixCodeWith("""
                unsafe struct IpAddressBuffer
                {
                    public System.Runtime.CompilerServices.InlineArray16<byte> IpAddress;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task FixedByteBuffer_InlineArray2Exists_UseCodeFix()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                unsafe struct IpAddressBuffer
                {
                    public fixed byte [||]IpAddress[2];
                }
                """)
            .ShouldFixCodeWith("""
                unsafe struct IpAddressBuffer
                {
                    public System.Runtime.CompilerServices.InlineArray2<byte> IpAddress;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task FixedByteBuffer_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                unsafe struct IpAddressBuffer
                {
                    public fixed byte [||]IpAddress[16];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task FixedIntBuffer_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                unsafe struct Buffer
                {
                    private fixed int [||]Values[8];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task FixedByteBuffer_Size17_NoCodeFix()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                unsafe struct IpAddressBuffer
                {
                    public fixed byte [||]IpAddress[17];
                }
                """)
            .ShouldFixCodeWith("""
                unsafe struct IpAddressBuffer
                {
                    public fixed byte IpAddress[17];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NonFixedField_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                struct Buffer
                {
                    private int Values;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task CSharp10_NoDiagnostic()
    {
        await new ProjectBuilder()
            .WithAnalyzer<UseInlineArrayInsteadOfFixedBufferAnalyzer>()
            .WithTargetFramework(TargetFramework.Net8_0)
            .WithLanguageVersion(LanguageVersion.CSharp10)
            .WithSourceCode("""
                unsafe struct IpAddressBuffer
                {
                    public fixed byte IpAddress[16];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Net7WithoutInlineArrayAttribute_NoDiagnostic()
    {
        await new ProjectBuilder()
            .WithAnalyzer<UseInlineArrayInsteadOfFixedBufferAnalyzer>()
            .WithTargetFramework(TargetFramework.Net7_0)
            .WithLanguageVersion(LanguageVersion.CSharp12)
            .WithSourceCode("""
                unsafe struct IpAddressBuffer
                {
                    public fixed byte IpAddress[16];
                }
                """)
            .ValidateAsync();
    }
}
#endif

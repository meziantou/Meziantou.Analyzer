using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseInlineArrayInsteadOfFixedBufferAnalyzer,
    Meziantou.Analyzer.Rules.UseInlineArrayInsteadOfFixedBufferFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseInlineArrayInsteadOfFixedBufferAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task FixedByteBuffer_InlineArray16Exists_UseCodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            unsafe struct IpAddressBuffer
            {
                public fixed byte {|MA0189:IpAddress|}[16];
            }
            """;
        test.FixedCode = """
            unsafe struct IpAddressBuffer
            {
                public System.Runtime.CompilerServices.InlineArray16<byte> IpAddress;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FixedByteBuffer_InlineArray2Exists_UseCodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            unsafe struct IpAddressBuffer
            {
                public fixed byte {|MA0189:IpAddress|}[2];
            }
            """;
        test.FixedCode = """
            unsafe struct IpAddressBuffer
            {
                public System.Runtime.CompilerServices.InlineArray2<byte> IpAddress;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FixedByteBuffer_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            unsafe struct IpAddressBuffer
            {
                public fixed byte {|MA0189:IpAddress|}[16];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FixedIntBuffer_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            unsafe struct Buffer
            {
                private fixed int {|MA0189:Values|}[8];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FixedByteBuffer_Size17_NoCodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            unsafe struct IpAddressBuffer
            {
                public fixed byte {|MA0189:IpAddress|}[17];
            }
            """;
        test.FixedCode = """
            unsafe struct IpAddressBuffer
            {
                public fixed byte {|MA0189:IpAddress|}[17];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonFixedField_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Buffer
            {
                private int Values;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CSharp10_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            unsafe struct IpAddressBuffer
            {
                public fixed byte IpAddress[16];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Net7WithoutInlineArrayAttribute_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = """
            unsafe struct IpAddressBuffer
            {
                public fixed byte IpAddress[16];
            }
            """;

        return test.RunAsync();
    }
}

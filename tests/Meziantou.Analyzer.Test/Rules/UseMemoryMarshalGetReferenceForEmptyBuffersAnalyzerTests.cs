using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseMemoryMarshalGetReferenceForEmptyBuffersAnalyzer,
    Meziantou.Analyzer.Rules.UseMemoryMarshalGetReferenceForEmptyBuffersFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseMemoryMarshalGetReferenceForEmptyBuffersAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task RefSpanArgument_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                static void M(ref byte b) { }
                void Test(Span<byte> span)
                {
                    M(ref [|span[0]|]);
                }
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                static void M(ref byte b) { }
                void Test(Span<byte> span)
                {
                    M(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(span));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InSpanArgument_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                static void M(in byte b) { }
                void Test(Span<byte> span)
                {
                    M(in [|span[0]|]);
                }
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                static void M(in byte b) { }
                void Test(Span<byte> span)
                {
                    M(in System.Runtime.InteropServices.MemoryMarshal.GetReference(span));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InReadOnlySpanArgument_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                static void M(in byte b) { }
                void Test(ReadOnlySpan<byte> span)
                {
                    M(in [|span[0]|]);
                }
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                static void M(in byte b) { }
                void Test(ReadOnlySpan<byte> span)
                {
                    M(in System.Runtime.InteropServices.MemoryMarshal.GetReference(span));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RefArrayArgument_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                static void M(ref byte b) { }
                void Test(byte[] array)
                {
                    M(ref [|array[0]|]);
                }
            }
            """;
        test.FixedCode = """
            class C
            {
                static void M(ref byte b) { }
                void Test(byte[] array)
                {
                    M(ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(array));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RefSpanReturn_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                ref byte Test(Span<byte> span)
                {
                    return ref [|span[0]|];
                }
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                ref byte Test(Span<byte> span)
                {
                    return ref System.Runtime.InteropServices.MemoryMarshal.GetReference(span);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RefLocalAssignment_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void Test(Span<byte> span)
                {
                    ref byte r = ref [|span[0]|];
                }
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                void Test(Span<byte> span)
                {
                    ref byte r = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(span);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RefSpanConstantZero_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                static void M(ref byte b) { }
                void Test(Span<byte> span)
                {
                    const int zero = 0;
                    M(ref [|span[zero]|]);
                }
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                static void M(ref byte b) { }
                void Test(Span<byte> span)
                {
                    const int zero = 0;
                    M(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(span));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValueAccessSpanNotByRef_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void Test(Span<byte> span)
                {
                    _ = span[0];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RefSpanNonZeroIndex_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                static void M(ref byte b) { }
                void Test(Span<byte> span)
                {
                    M(ref span[1]);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RefArrayNonZeroIndex_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                static void M(ref byte b) { }
                void Test(byte[] array)
                {
                    M(ref array[1]);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RefNonConstantIndex_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                static void M(ref byte b) { }
                void Test(Span<byte> span, int index)
                {
                    M(ref span[index]);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RefCustomRefReturningIndexer_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class C
            {
                static void M(ref int v) { }
                void Test(MyCollection col)
                {
                    M(ref col[0]);
                }
            }
            struct MyCollection
            {
                private int[] _items;
                public ref int this[int index] => ref _items[index];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArrayOnNet5_DiagnosticFiresButNoCodeFix()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net50;
        test.TestCode = """
            class C
            {
                static void M(ref byte b) { }
                void Test(byte[] array)
                {
                    M(ref [|array[0]|]);
                }
            }
            """;

        return test.RunAsync();
    }
}

using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseMemoryMarshalGetReferenceForEmptyBuffersAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseMemoryMarshalGetReferenceForEmptyBuffersAnalyzer>()
            .WithCodeFixProvider<UseMemoryMarshalGetReferenceForEmptyBuffersFixer>()
            .WithTargetFramework(TargetFramework.Net6_0);
    }

    [Fact]
    public async Task RefSpanArgument_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      static void M(ref byte b) { }
                      void Test(Span<byte> span)
                      {
                          M(ref [|span[0]|]);
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  class C
                  {
                      static void M(ref byte b) { }
                      void Test(Span<byte> span)
                      {
                          M(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(span));
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task InSpanArgument_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      static void M(in byte b) { }
                      void Test(Span<byte> span)
                      {
                          M(in [|span[0]|]);
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  class C
                  {
                      static void M(in byte b) { }
                      void Test(Span<byte> span)
                      {
                          M(in System.Runtime.InteropServices.MemoryMarshal.GetReference(span));
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task InReadOnlySpanArgument_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      static void M(in byte b) { }
                      void Test(ReadOnlySpan<byte> span)
                      {
                          M(in [|span[0]|]);
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  class C
                  {
                      static void M(in byte b) { }
                      void Test(ReadOnlySpan<byte> span)
                      {
                          M(in System.Runtime.InteropServices.MemoryMarshal.GetReference(span));
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefArrayArgument_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class C
                  {
                      static void M(ref byte b) { }
                      void Test(byte[] array)
                      {
                          M(ref [|array[0]|]);
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  class C
                  {
                      static void M(ref byte b) { }
                      void Test(byte[] array)
                      {
                          M(ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(array));
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefSpanReturn_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      ref byte Test(Span<byte> span)
                      {
                          return ref [|span[0]|];
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  class C
                  {
                      ref byte Test(Span<byte> span)
                      {
                          return ref System.Runtime.InteropServices.MemoryMarshal.GetReference(span);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefLocalAssignment_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void Test(Span<byte> span)
                      {
                          ref byte r = ref [|span[0]|];
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System;
                  class C
                  {
                      void Test(Span<byte> span)
                      {
                          ref byte r = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(span);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefSpanConstantZero_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
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
                  """)
              .ShouldFixCodeWith("""
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
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ValueAccessSpanNotByRef_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      void Test(Span<byte> span)
                      {
                          _ = span[0];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefSpanNonZeroIndex_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      static void M(ref byte b) { }
                      void Test(Span<byte> span)
                      {
                          M(ref span[1]);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefArrayNonZeroIndex_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class C
                  {
                      static void M(ref byte b) { }
                      void Test(byte[] array)
                      {
                          M(ref array[1]);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefNonConstantIndex_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System;
                  class C
                  {
                      static void M(ref byte b) { }
                      void Test(Span<byte> span, int index)
                      {
                          M(ref span[index]);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefCustomRefReturningIndexer_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
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
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ArrayOnNet5_DiagnosticFiresButNoCodeFix()
    {
        await new ProjectBuilder()
              .WithAnalyzer<UseMemoryMarshalGetReferenceForEmptyBuffersAnalyzer>()
              .WithCodeFixProvider<UseMemoryMarshalGetReferenceForEmptyBuffersFixer>()
              .WithTargetFramework(TargetFramework.Net5_0)
              .WithSourceCode("""
                  class C
                  {
                      static void M(ref byte b) { }
                      void Test(byte[] array)
                      {
                          M(ref [|array[0]|]);
                      }
                  }
                  """)
              .ValidateAsync();
    }
}

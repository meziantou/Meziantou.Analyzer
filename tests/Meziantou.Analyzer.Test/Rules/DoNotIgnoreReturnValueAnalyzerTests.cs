using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotIgnoreReturnValueAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotIgnoreReturnValueAnalyzer>()
            .AddMeziantouAttributes();
    }

    [Fact]
    public async Task Stream_Read_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.IO;
                  class Test
                  {
                      void A()
                      {
                          var stream = File.OpenRead("");
                          [|stream.Read(null, 0, 0)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Stream_ReadAsync_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.IO;
                  class Test
                  {
                      async void A()
                      {
                          var stream = File.OpenRead("");
                          await [|stream.ReadAsync(null, 0, 0)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Stream_ReadAsync_ReturnValueUsed_DiscardOperator()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.IO;
                  class Test
                  {
                      async void A()
                      {
                          var stream = File.OpenRead("");
                          _ = await stream.ReadAsync(null, 0, 0);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Stream_Read_ReturnValueUsed_MethodCall()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.IO;
                  class Test
                  {
                      void A()
                      {
                          var stream = File.OpenRead("");
                          System.Console.Write(stream.Read(null, 0, 0));
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Stream_ReadByte_ReturnValueNotUsed_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.IO;
                  class Test
                  {
                      void A()
                      {
                          var stream = File.OpenRead("");
                          stream.ReadByte();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task TextReader_ReadLine_ReturnValueNotUsed_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.IO;
                  class Test
                  {
                      void A()
                      {
                          var reader = new StringReader("test");
                          reader.ReadLine();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task TextReader_ReadLine_ReturnValueUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.IO;
                  class Test
                  {
                      void A()
                      {
                          var reader = new StringReader("test");
                          var line = reader.ReadLine();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task BinaryReader_ReadInt32_ReturnValueNotUsed_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.IO;
                  class Test
                  {
                      void A(BinaryReader reader)
                      {
                          reader.ReadInt32();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Attribute_ReturnValue_NotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using Meziantou.Analyzer.Annotations;
                  class Test
                  {
                      [return: DoNotIgnore]
                      static int Compute() => 0;

                      void A()
                      {
                          [|Compute()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Attribute_ReturnValue_Used()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using Meziantou.Analyzer.Annotations;
                  class Test
                  {
                      [return: DoNotIgnore]
                      static int Compute() => 0;

                      void A()
                      {
                          var result = Compute();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Attribute_ReturnValue_WithMessage()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using Meziantou.Analyzer.Annotations;
                  class Test
                  {
                      [return: DoNotIgnore(Message = "Use the result to check success")]
                      static int Compute() => 0;

                      void A()
                      {
                          [|Compute()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Attribute_OutParameter_Discarded()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using Meziantou.Analyzer.Annotations;
                  class Test
                  {
                      static bool TryGet([DoNotIgnore] out int value) { value = 0; return true; }

                      void A()
                      {
                          TryGet({|MA0060:out _|});
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Attribute_OutParameter_NotDiscarded()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using Meziantou.Analyzer.Annotations;
                  class Test
                  {
                      static bool TryGet([DoNotIgnore] out int value) { value = 0; return true; }

                      void A()
                      {
                          TryGet(out int x);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Pure_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Diagnostics.Contracts;
                  class Test
                  {
                      [Pure]
                      static int Compute() => 0;

                      void A()
                      {
                          [|Compute()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Pure_ReturnValueUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Diagnostics.Contracts;
                  class Test
                  {
                      [Pure]
                      static int Compute() => 0;

                      void A()
                      {
                          var result = Compute();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Pure_OnClass_NoMethodDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Diagnostics.Contracts;
                  [Pure]
                  class MyClass
                  {
                      public int Compute() => 0;
                  }
                  class Test
                  {
                      void A(MyClass obj)
                      {
                          obj.Compute();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task String_Trim_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A(string s)
                      {
                          [|s.Trim()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task String_Trim_ReturnValueUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A(string s)
                      {
                          var trimmed = s.Trim();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task String_Replace_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A(string s)
                      {
                          [|s.Replace("a", "b")|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task String_Format_ArrowVoidMethod_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A() => [|string.Format("{0}", 1)|];
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task String_Format_ArrowStringMethod_ReturnValueUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      string A() => string.Format("{0}", 1);
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task TryParse_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A()
                      {
                          [|int.TryParse("42", out _)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task TryParse_ReturnValueUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A()
                      {
                          if (int.TryParse("42", out var value)) { }
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task TryParse_CustomMethod_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      static bool TryParseItem(string s, out int result) { result = 0; return true; }

                      void A()
                      {
                          [|TryParseItem("42", out _)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task TryParse_ReturnValueNotUsed_DisabledUsingConfiguration()
    {
        await CreateProjectBuilder()
              .WithAnalyzerConfiguration(new Dictionary<string, string>
              {
                  ["MA0060.enable_tryparse_pattern"] = "false",
              })
              .WithSourceCode("""
                  class Test
                  {
                      void A()
                      {
                          int.TryParse("42", out _);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task TryParse_ReturnValueNotUsed_InvalidConfiguration_UsesDefaultValue()
    {
        await CreateProjectBuilder()
              .WithAnalyzerConfiguration(new Dictionary<string, string>
              {
                  ["MA0060.enable_tryparse_pattern"] = "invalid",
              })
              .WithSourceCode("""
                  class Test
                  {
                      void A()
                      {
                          [|int.TryParse("42", out _)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableList_Add_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Immutable;
                  class Test
                  {
                      void A(ImmutableList<int> list)
                      {
                          [|list.Add(1)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableList_Add_ReturnValueUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Immutable;
                  class Test
                  {
                      void A(ImmutableList<int> list)
                      {
                          var newList = list.Add(1);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableDictionary_Remove_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Immutable;
                  class Test
                  {
                      void A(ImmutableDictionary<string, int> dict)
                      {
                          [|dict.Remove("key")|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableStack_Push_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Immutable;
                  class Test
                  {
                      void A(ImmutableStack<int> stack)
                      {
                          [|stack.Push(1)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableArray_Create_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Immutable;
                  class Test
                  {
                      void A()
                      {
                          [|ImmutableArray.Create(1, 2, 3)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Stream_ReadAtLeast_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.IO;
                  class Test
                  {
                      void A(Stream stream, byte[] buffer)
                      {
                          [|stream.ReadAtLeast(buffer, 1)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task TextReader_Read_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.IO;
                  class Test
                  {
                      void A(TextReader reader)
                      {
                          [|reader.Read()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task BinaryReader_Read_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.IO;
                  class Test
                  {
                      void A(BinaryReader reader)
                      {
                          [|reader.Read()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task String_ToUpper_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A(string s)
                      {
                          [|s.ToUpper()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task String_Join_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A()
                      {
                          [|string.Join(", ", "a", "b")|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableDictionary_Add_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Immutable;
                  class Test
                  {
                      void A(ImmutableDictionary<string, int> dict)
                      {
                          [|dict.Add("key", 1)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableQueue_Enqueue_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Immutable;
                  class Test
                  {
                      void A(ImmutableQueue<int> queue)
                      {
                          [|queue.Enqueue(1)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableSet_Add_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Immutable;
                  class Test
                  {
                      void A(ImmutableHashSet<int> set)
                      {
                          [|set.Add(1)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImmutableArrayBuilder_IndexOf_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Collections.Immutable;
                  class Test
                  {
                      void A(ImmutableArray<int>.Builder builder)
                      {
                          [|builder.IndexOf(1)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HResult_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A()
                      {
                          [|NativeMethod()|];
                      }

                      Windows.Win32.Foundation.HRESULT NativeMethod() => default;
                  }

                  namespace Windows.Win32.Foundation
                  {
                      public struct HRESULT { }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HResult_ReturnValueUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A()
                      {
                          var hr = NativeMethod();
                      }

                      Windows.Win32.Foundation.HRESULT NativeMethod() => default;
                  }

                  namespace Windows.Win32.Foundation
                  {
                      public struct HRESULT { }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AssemblyAttribute_SimpleMethod_NotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  [assembly: Meziantou.Analyzer.Annotations.DoNotIgnore("M:Test.Sample")]
                  class Test
                  {
                      static int Sample() => 42;

                      void A()
                      {
                          [|Sample()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AssemblyAttribute_SimpleMethod_Used()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  [assembly: Meziantou.Analyzer.Annotations.DoNotIgnore("M:Test.Sample")]
                  class Test
                  {
                      static int Sample() => 42;

                      void A()
                      {
                          var value = Sample();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AssemblyAttribute_NestedTypeMethod_NotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  [assembly: Meziantou.Analyzer.Annotations.DoNotIgnore("M:Test.Nested.Sample")]
                  class Test
                  {
                      class Nested
                      {
                          public static int Sample() => 42;
                      }

                      void A()
                      {
                          [|Nested.Sample()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AssemblyAttribute_GenericTypeMethod_NotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  [assembly: Meziantou.Analyzer.Annotations.DoNotIgnore("M:Test`1.Sample")]
                  class Test<T>
                  {
                      static int Sample() => 42;

                      void A()
                      {
                          [|Sample()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AssemblyAttribute_GenericMethod_NotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  [assembly: Meziantou.Analyzer.Annotations.DoNotIgnore("M:Test.Sample``1")]
                  class Test
                  {
                      static int Sample<T>() => 42;

                      void A()
                      {
                          [|Sample<int>()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AssemblyAttribute_MultipleEntries_NotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  [assembly: Meziantou.Analyzer.Annotations.DoNotIgnore("M:Test.SampleA")]
                  [assembly: Meziantou.Analyzer.Annotations.DoNotIgnore("M:Test.SampleB")]
                  class Test
                  {
                      static int SampleA() => 1;
                      static int SampleB() => 2;

                      void A()
                      {
                          [|SampleA()|];
                          [|SampleB()|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

}

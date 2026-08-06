using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotIgnoreReturnValueAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotIgnoreReturnValueAnalyzer>();
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
                  """ + DoNotIgnoreAttributeSource)
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
                  """ + DoNotIgnoreAttributeSource)
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
                  """ + DoNotIgnoreAttributeSource)
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
                  """ + DoNotIgnoreAttributeSource)
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
                  """ + DoNotIgnoreAttributeSource)
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

    private const string DoNotIgnoreAttributeSource = """

        namespace Meziantou.Analyzer.Annotations
        {
            [System.AttributeUsage(System.AttributeTargets.ReturnValue | System.AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
            public sealed class DoNotIgnoreAttribute : System.Attribute
            {
                public DoNotIgnoreAttribute() {}
                public string? Message { get; set; }
            }
        }
        """;
}

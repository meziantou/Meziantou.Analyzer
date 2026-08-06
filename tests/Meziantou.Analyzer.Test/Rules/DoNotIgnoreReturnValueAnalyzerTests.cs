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
    public async Task Stream_ReadByte_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.IO;
                  class Test
                  {
                      void A()
                      {
                          var stream = File.OpenRead("");
                          [|stream.ReadByte()|];
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
    public async Task BinaryReader_ReadInt32_ReturnValueNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.IO;
                  class Test
                  {
                      void A(BinaryReader reader)
                      {
                          [|reader.ReadInt32()|];
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
                          [|TryGet(out _)|];
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

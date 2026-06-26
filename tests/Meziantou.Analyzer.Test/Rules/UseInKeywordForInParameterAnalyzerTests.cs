using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseInKeywordForInParameterAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseInKeywordForInParameterAnalyzer>()
            .WithCodeFixProvider<UseInKeywordForInParameterFixer>();
    }

    [Fact]
    public async Task StyleRule_Variable_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class C
                  {
                      public void Test()
                      {
                          var value = new S();
                          M({|MA0209:value|});
                      }

                      private static void M(in S value) { }
                  }

                  struct S { }
                  """)
              .ShouldFixCodeWith("""
                  class C
                  {
                      public void Test()
                      {
                          var value = new S();
                          M(in value);
                      }

                      private static void M(in S value) { }
                  }

                  struct S { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task StyleRule_AlreadyIn_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class C
                  {
                      public void Test()
                      {
                          var value = new S();
                          M(in value);
                      }

                      private static void M(in S value) { }
                  }

                  struct S { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task StyleRule_Literal_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class C
                  {
                      public void Test()
                      {
                          M(42);
                      }

                      private static void M(in int value) { }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task StyleRule_Property_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  struct S { }

                  class C
                  {
                      public S Property => default;

                      public void Test()
                      {
                          M(Property);
                      }

                      private static void M(in S value) { }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task StyleRule_MethodReturnValue_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class C
                  {
                      public void Test()
                      {
                          M(GetValue());
                      }

                      private static S GetValue() => default;
                      private static void M(in S value) { }
                  }

                  struct S { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task StyleRule_ImplicitConversion_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class C
                  {
                      public void Test()
                      {
                          short value = 0;
                          M(value);
                      }

                      private static void M(in int value) { }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task StyleRule_Expression_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class C
                  {
                      public void Test()
                      {
                          var a = 1;
                          var b = 2;
                          M(a + b);
                      }

                      private static void M(in int value) { }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task StyleRule_ObjectCreation_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class C
                  {
                      public void Test()
                      {
                          M(new S());
                      }

                      private static void M(in S value) { }
                  }

                  struct S { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task OverloadRule_Variable_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class C
                  {
                      public void Test()
                      {
                          var value = new S();
                          M({|MA0210:value|});
                      }

                      private static void M(S value) { }
                      private static void M(in S value) { }
                  }

                  struct S { }
                  """)
              .ShouldFixCodeWith("""
                  class C
                  {
                      public void Test()
                      {
                          var value = new S();
                          M(in value);
                      }

                      private static void M(S value) { }
                      private static void M(in S value) { }
                  }

                  struct S { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task OverloadRule_Expression_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class C
                  {
                      public void Test()
                      {
                          M(new S());
                      }

                      private static void M(S value) { }
                      private static void M(in S value) { }
                  }

                  struct S { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task OverloadRule_ImplicitConversion_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class C
                  {
                      public void Test()
                      {
                          short value = 0;
                          M(value);
                      }

                      private static void M(int value) { }
                      private static void M(in int value) { }
                  }
                  """)
              .ValidateAsync();
    }
}

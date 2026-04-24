using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseAnOverloadThatHasMidpointRoundingAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseAnOverloadThatHasMidpointRoundingAnalyzer>()
            .WithCodeFixProvider<UseAnOverloadThatHasMidpointRoundingFixer>();
    }

    [Fact]
    public async Task MathRoundWithoutMode_ReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A()
                      {
                          _ = [|System.Math.Round(2.5)|];
                          _ = [|System.Math.Round(2.5, 1)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task MathRoundWithMode_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A()
                      {
                          _ = System.Math.Round(2.5, System.MidpointRounding.AwayFromZero);
                          _ = System.Math.Round(2.5, 1, System.MidpointRounding.AwayFromZero);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Theory]
    [InlineData(0, "ToEven")]
    [InlineData(1, "AwayFromZero")]
    [InlineData(2, "ToZero")]
    [InlineData(3, "ToNegativeInfinity")]
    [InlineData(4, "ToPositiveInfinity")]
    public async Task MathRound_CodeFix_SuggestsEachMidpointRoundingValue(int codeFixIndex, string midpointRoundingMember)
    {
        var fixedCode = $$"""
            class Test
            {
                void A()
                {
                    _ = System.Math.Round(2.5, System.MidpointRounding.{{midpointRoundingMember}});
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A()
                      {
                          _ = [|System.Math.Round(2.5)|];
                      }
                  }
                  """)
              .ShouldFixCodeWith(codeFixIndex, fixedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MathFRoundWithoutMode_ReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A()
                      {
                          _ = [|System.MathF.Round(2.5f)|];
                          _ = [|System.MathF.Round(2.5f, 1)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task MathFRoundWithMode_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A()
                      {
                          _ = System.MathF.Round(2.5f, System.MidpointRounding.AwayFromZero);
                          _ = System.MathF.Round(2.5f, 1, System.MidpointRounding.AwayFromZero);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task DecimalRoundWithoutMode_ReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A(decimal value)
                      {
                          _ = [|decimal.Round(value)|];
                          _ = [|decimal.Round(value, 1)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task DecimalRoundWithMode_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A(decimal value)
                      {
                          _ = decimal.Round(value, System.MidpointRounding.AwayFromZero);
                          _ = decimal.Round(value, 1, System.MidpointRounding.AwayFromZero);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Theory]
    [InlineData(0, "ToEven")]
    [InlineData(1, "AwayFromZero")]
    [InlineData(2, "ToZero")]
    [InlineData(3, "ToNegativeInfinity")]
    [InlineData(4, "ToPositiveInfinity")]
    public async Task DecimalRound_CodeFix_SuggestsEachMidpointRoundingValue(int codeFixIndex, string midpointRoundingMember)
    {
        var fixedCode = $$"""
            class Test
            {
                void A(decimal value)
                {
                    _ = decimal.Round(value, 1, System.MidpointRounding.{{midpointRoundingMember}});
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A(decimal value)
                      {
                          _ = [|decimal.Round(value, 1)|];
                      }
                  }
                  """)
              .ShouldFixCodeWith(codeFixIndex, fixedCode)
              .ValidateAsync();
    }

#if CSHARP11_OR_GREATER
    [Fact]
    public async Task FloatingPointImplementationsRoundWithoutMode_ReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11)
              .WithSourceCode("""
                  class Test
                  {
                      void A(double d, float f, System.Half h)
                      {
                          _ = [|double.Round(d)|];
                          _ = [|double.Round(d, 1)|];
                          _ = [|float.Round(f)|];
                          _ = [|float.Round(f, 1)|];
                          _ = [|System.Half.Round(h)|];
                          _ = [|System.Half.Round(h, 1)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task FloatingPointImplementationsRoundWithMode_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11)
              .WithSourceCode("""
                  class Test
                  {
                      void A(double d, float f, System.Half h)
                      {
                          _ = double.Round(d, System.MidpointRounding.AwayFromZero);
                          _ = double.Round(d, 1, System.MidpointRounding.AwayFromZero);
                          _ = float.Round(f, System.MidpointRounding.AwayFromZero);
                          _ = float.Round(f, 1, System.MidpointRounding.AwayFromZero);
                          _ = System.Half.Round(h, System.MidpointRounding.AwayFromZero);
                          _ = System.Half.Round(h, 1, System.MidpointRounding.AwayFromZero);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task IFloatingPointRoundWithoutMode_ReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11)
              .WithSourceCode("""
                  using System.Numerics;

                  class Test
                  {
                      static T Round<T>(T value) where T : IFloatingPoint<T>
                      {
                          _ = [|T.Round(value)|];
                          return [|T.Round(value, 1)|];
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task IFloatingPointRoundWithMode_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11)
              .WithSourceCode("""
                  using System.Numerics;

                  class Test
                  {
                      static T Round<T>(T value) where T : IFloatingPoint<T>
                      {
                          _ = T.Round(value, System.MidpointRounding.AwayFromZero);
                          return T.Round(value, 1, System.MidpointRounding.AwayFromZero);
                      }
                  }
                  """)
              .ValidateAsync();
    }
#endif
}

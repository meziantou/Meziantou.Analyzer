using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseEmptyPropertyPatternOnNonNullableValueTypeAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
        => new ProjectBuilder()
            .WithAnalyzer<DoNotUseEmptyPropertyPatternOnNonNullableValueTypeAnalyzer>();

    [Fact]
    public async Task IsEmptyPropertyPattern_NonNullableValueType_ReportDiagnostic()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private static bool A()
                      {
                          int value = 0;
                          return [|value is { }|];
                      }
                  }
                  """)
              .ValidateAsync();

    [Fact]
    public async Task IsEmptyPropertyPattern_ConstrainedGenericValueType_ReportDiagnostic()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private static bool A<T>(T value) where T : struct => [|value is { }|];
                  }
                  """)
              .ValidateAsync();

    [Fact]
    public async Task IsEmptyPropertyPattern_NullableValueType_NoDiagnostic()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private static bool A()
                      {
                          int? value = 0;
                          return value is { };
                      }
                  }
                  """)
              .ValidateAsync();

    [Fact]
    public async Task IsNotEmptyPropertyPattern_NullableValueType_NoDiagnostic()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private static bool A()
                      {
                          int? value = 0;
                          return value is not { };
                      }
                  }
                  """)
              .ValidateAsync();

    [Fact]
    public async Task IsEmptyPropertyPattern_ReferenceType_NoDiagnostic()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private static bool A()
                      {
                          object value = 0;
                          return value is { };
                      }
                  }
                  """)
              .ValidateAsync();

    [Fact]
    public async Task IsEmptyPropertyPattern_UnconstrainedGenericType_NoDiagnostic()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private static bool A<T>(T value) => value is { };
                  }
                  """)
              .ValidateAsync();
}

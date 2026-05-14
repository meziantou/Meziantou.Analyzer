using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseEmptyPropertyPatternOnNonNullableValueTypeAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
        => new ProjectBuilder()
            .WithAnalyzer<DoNotUseEmptyPropertyPatternOnNonNullableValueTypeAnalyzer>()
            .WithCodeFixProvider<DoNotUseEmptyPropertyPatternOnNonNullableValueTypeFixer>();

    [Fact]
    public async Task IsEmptyPropertyPattern_NonNullableValueType_ReportDiagnostic()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private static bool A()
                      {
                          int value = 0;
                          return value is [|{ }|];
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
                      private static bool A<T>(T value) where T : struct => value is [|{ }|];
                  }
                  """)
              .ValidateAsync();

    [Fact]
    public async Task IsEmptyPropertyPatternWithDesignation_NonNullableValueType_CodeFix()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private static bool A()
                      {
                          int value = 0;
                          return value is [|{ } newName|];
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  class Sample
                  {
                      private static bool A()
                      {
                          int value = 0;
                          return value is var newName;
                      }
                  }
                  """)
              .ValidateAsync();

    [Fact]
    public async Task IsEmptyPropertyPattern_NestedPropertyPattern_ReportDiagnostic()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private sealed class Nested
                      {
                          public int Value { get; set; }
                      }

                      private static bool A(Nested value)
                      {
                          return value is { Value: [|{ }|] };
                      }
                  }
                  """)
              .ValidateAsync();

    [Fact]
    public async Task IsEmptyPropertyPatternWithDesignation_NestedPropertyPattern_CodeFix()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private sealed class Nested
                      {
                          public int Value { get; set; }
                      }

                      private static bool A(Nested value)
                      {
                          return value is { Value: [|{ } newName|] };
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  class Sample
                  {
                      private sealed class Nested
                      {
                          public int Value { get; set; }
                      }

                      private static bool A(Nested value)
                      {
                          return value is { Value: var newName };
                      }
                  }
                  """)
              .ValidateAsync();

    [Fact]
    public async Task IsEmptyPropertyPatternWithDesignation_MultipleNestedPropertyPatterns_BatchCodeFix()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private sealed class Nested
                      {
                          public int Value1 { get; set; }
                          public int Value2 { get; set; }
                      }

                      private static bool A(Nested value)
                      {
                          return value is { Value1: [|{ } name1|], Value2: [|{ } name2|] };
                      }
                  }
                  """)
              .ShouldBatchFixCodeWith("""
                  class Sample
                  {
                      private sealed class Nested
                      {
                          public int Value1 { get; set; }
                          public int Value2 { get; set; }
                      }

                      private static bool A(Nested value)
                      {
                          return value is { Value1: var name1, Value2: var name2 };
                      }
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

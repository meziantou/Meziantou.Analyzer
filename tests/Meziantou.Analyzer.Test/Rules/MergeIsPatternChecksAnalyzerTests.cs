using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MergeIsPatternChecksAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithAnalyzer<MergeIsPatternChecksAnalyzer>()
            .WithCodeFixProvider<MergeIsPatternChecksFixer>();
    }

    [Fact]
    public async Task LogicalOr_ConstantPattern()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  var value = 0;
                  _ = [|value is 1 || value is 2|];
                  """)
              .ShouldFixCodeWith("""
                  var value = 0;
                  _ = value is 1 or 2;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LogicalOr_EnumPattern()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  var value = (System.DayOfWeek)0;
                  _ = [|value is System.DayOfWeek.Monday || value is System.DayOfWeek.Tuesday|];
                  """)
              .ShouldFixCodeWith("""
                  var value = (System.DayOfWeek)0;
                  _ = value is System.DayOfWeek.Monday or System.DayOfWeek.Tuesday;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LogicalAnd_EnumPattern()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  var value = (System.DayOfWeek)0;
                  _ = [|value is System.DayOfWeek.Monday && value is System.DayOfWeek.Tuesday|];
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LogicalAnd_NotPattern()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  var value = (System.DayOfWeek)0;
                  _ = [|value is System.DayOfWeek.Monday && value is not System.DayOfWeek.Tuesday|];
                  """)
              .ShouldFixCodeWith("""
                  var value = (System.DayOfWeek)0;
                  _ = value is System.DayOfWeek.Monday and not System.DayOfWeek.Tuesday;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LogicalAnd_ParenthesizeOrPattern()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  var value = MyEnum.Value1;
                  _ = [|value is (MyEnum.Value1 or MyEnum.Value2) && value is not MyEnum.Value2|];

                  enum MyEnum { Value1, Value2 }
                  """)
              .ShouldFixCodeWith("""
                  var value = MyEnum.Value1;
                  _ = value is (MyEnum.Value1 or MyEnum.Value2) and not MyEnum.Value2;

                  enum MyEnum { Value1, Value2 }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task DifferentExpressions_DoNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  var value1 = MyEnum.Value1;
                  var value2 = MyEnum.Value2;
                  _ = value1 is MyEnum.Value1 || value2 is MyEnum.Value2;

                  enum MyEnum { Value1, Value2 }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AlreadyMerged_DoNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  var value = MyEnum.Value1;
                  _ = value is MyEnum.Value1 or MyEnum.Value2;

                  enum MyEnum { Value1, Value2 }
                  """)
              .ValidateAsync();
    }
}

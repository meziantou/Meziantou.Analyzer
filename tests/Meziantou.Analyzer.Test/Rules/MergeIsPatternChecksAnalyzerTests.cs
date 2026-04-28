using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
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

    [Fact]
    public async Task Parameter()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  static bool M(int value) => [|value is 1 || value is 2|];
                  """)
              .ShouldFixCodeWith("""
                  static bool M(int value) => value is 1 or 2;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LocalVariable()
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
    public async Task Field_ExplicitAndImplicitThis()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  _ = new Sample().M();

                  class Sample
                  {
                      private int _value;
                      public bool M() => [|_value is 1 || this._value is 2|];
                  }
                  """)
              .ShouldFixCodeWith("""
                  _ = new Sample().M();

                  class Sample
                  {
                      private int _value;
                      public bool M() => _value is 1 or 2;
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Property_ExplicitAndImplicitThis()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  _ = new Sample().M();

                  class Sample
                  {
                      private int Value { get; set; }
                      public bool M() => [|Value is 1 || this.Value is 2|];
                  }
                  """)
              .ShouldFixCodeWith("""
                  _ = new Sample().M();

                  class Sample
                  {
                      private int Value { get; set; }
                      public bool M() => Value is 1 or 2;
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Property_DifferentInstances_DoNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  var a = new Sample();
                  var b = new Sample();
                  _ = a.Value is 1 || b.Value is 2;

                  class Sample
                  {
                      public int Value { get; set; }
                  }
                  """)
              .ValidateAsync();
    }

#if CSHARP12_OR_GREATER
    [Fact]
    public async Task PrimaryConstructorParameter()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .WithSourceCode("""
                  _ = new Sample(1).M();

                  class Sample(int value)
                  {
                      public bool M() => [|value is 1 || value is 2|];
                  }
                  """)
              .ShouldFixCodeWith("""
                  _ = new Sample(1).M();

                  class Sample(int value)
                  {
                      public bool M() => value is 1 or 2;
                  }
                  """)
              .ValidateAsync();
    }
#endif

    [Fact]
    public void Rule_SeverityAndDefault()
    {
        var rule = new MergeIsPatternChecksAnalyzer().SupportedDiagnostics[0];
        Assert.Equal(DiagnosticSeverity.Info, rule.DefaultSeverity);
        Assert.True(rule.IsEnabledByDefault);
    }
}

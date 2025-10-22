using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UsePatternMatchingForEqualityComparisonsAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithAnalyzer<UsePatternMatchingForEqualityComparisonsAnalyzer>()
            .WithCodeFixProvider<UsePatternMatchingForEqualityComparisonsFixer>();
    }

    [Fact]
    public async Task DisabledInExpression()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""              
                  using System;
                  using System.Linq;
                  using System.Linq.Expressions;
                  _ = (Expression<Func<int, bool>>)(item => item == 0);
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NullCheckForNullableOfT()
    {
        await CreateProjectBuilder()
              .WithSourceCode("_ = [|(int?)0 == null|];")
              .ShouldFixCodeWith("_ = (int?)0 is null;")
              .ValidateAsync();
    }

    [Fact]
    public async Task NullCheckForNullableOfT_NotNull()
    {
        await CreateProjectBuilder()
              .WithSourceCode("_ = [|(int?)0 != null|];")
              .ShouldFixCodeWith("_ = (int?)0 is not null;")
              .ValidateAsync();
    }

    [Fact]
    public async Task NullCheckForObject()
    {
        await CreateProjectBuilder()
              .WithSourceCode("_ = [|new object() == null|];")
              .ShouldFixCodeWith("_ = new object() is null;")
              .ValidateAsync();
    }

    [Fact]
    public async Task NullCheckForObject_NullFirst()
    {
        await CreateProjectBuilder()
              .WithSourceCode("_ = [|null == new object()|];")
              .ShouldFixCodeWith("_ = new object() is null;")
              .ValidateAsync();
    }

    [Fact]
    public async Task NullCheckForObject_NotNull_NullFirst()
    {
        await CreateProjectBuilder()
              .WithSourceCode("_ = [|null != new object()|];")
              .ShouldFixCodeWith("_ = new object() is not null;")
              .ValidateAsync();
    }

    [Fact]
    public async Task NullCheckForObject_FixerKeepParentheses()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  string line;
                  while ([|(line = null) != null|]) { }
                  """)
              .ShouldFixCodeWith("""
                  string line;
                  while ((line = null) is not null) { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NullEqualsNull()
    {
        // no report as "null is null" is not valid
        await CreateProjectBuilder()
              .WithSourceCode("_ = null == null;")
              .ValidateAsync();
    }

    [Fact]
    public async Task NotNullCheck()
    {
        // no report as "null is null" is not valid
        await CreateProjectBuilder()
              .WithSourceCode("_ = new object() == new object();")
              .ValidateAsync();
    }

    [Fact]
    public async Task NullCheckForObjectWithCustomOperator()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                _ = new Sample() == null;

                class Sample
                {
                    public static bool operator ==(Sample left, Sample right) => false;
                    public static bool operator !=(Sample left, Sample right) => false;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NullCheckForNullableOfT_IsNull()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"_ = (int?)0 is null;")
              .ValidateAsync();
    }

    [Fact]
    public async Task EqualityComparison_String()
    {
        await CreateProjectBuilder()
              .WithSourceCode($"""_ = [|(string)"dummy" == "dummy"|];""")
              .ShouldFixCodeWith($"""_ = (string)"dummy" is "dummy";""")
              .ValidateAsync();
    }

    [Fact]
    public async Task EqualityComparison_NullableInt32_Int32()
    {
        await CreateProjectBuilder()
              .WithSourceCode($"_ = [|(int?)0 == 1|];")
              .ShouldFixCodeWith($"_ = (int?)0 is 1;")
              .ValidateAsync();
    }

    [Fact]
    public async Task EqualityComparison_Enum()
    {
        await CreateProjectBuilder()
              .WithSourceCode($"_ = [|(System.DayOfWeek)1 == System.DayOfWeek.Monday|];")
              .ShouldFixCodeWith($"_ = (System.DayOfWeek)1 is System.DayOfWeek.Monday;")
              .ValidateAsync();
    }

    [Fact]
    public async Task EqualityComparison_NullableEnum()
    {
        await CreateProjectBuilder()
              .WithSourceCode($"_ = [|(System.DayOfWeek?)1 == System.DayOfWeek.Monday|];")
              .ShouldFixCodeWith($"_ = (System.DayOfWeek?)1 is System.DayOfWeek.Monday;")
              .ValidateAsync();
    }

    [Fact]
    public async Task CustomOperator_Class_Int32()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                _ = new Sample() == 1;

                class Sample
                {
                    public static bool operator ==(Sample left, int right) => false;
                    public static bool operator !=(Sample left, int right) => false;
                }
                """)
              .ValidateAsync();
    }
}

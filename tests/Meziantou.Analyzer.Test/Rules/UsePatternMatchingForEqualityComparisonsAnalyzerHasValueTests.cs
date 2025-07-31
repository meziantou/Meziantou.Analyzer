using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UsePatternMatchingForEqualityComparisonsAnalyzerHasValueTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithAnalyzer<UsePatternMatchingInsteadOfHasValueAnalyzer>()
            .WithCodeFixProvider<UsePatternMatchingInsteadOfHasvalueFixer>();
    }

    [Fact]
    public async Task HasValue()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""              
                  var value = default(int?);
                  _ = [|value.HasValue|];
                  """)
              .ShouldFixCodeWith("""
                  var value = default(int?);
                  _ = value is not null;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NotHasValue()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""              
                  var value = default(int?);
                  _ = ![|value.HasValue|];
                  """)
              .ShouldFixCodeWith("""
                  var value = default(int?);
                  _ = value is null;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HasValueEqualsTrue()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""              
                  var value = default(int?);
                  _ = [|value.HasValue|] == true;
                  """)
              .ShouldFixCodeWith("""
                  var value = default(int?);
                  _ = value is not null;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HasValueEqualsFalse()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""              
                  var value = default(int?);
                  _ = [|value.HasValue|] == false;
                  """)
              .ShouldFixCodeWith("""
                  var value = default(int?);
                  _ = value is null;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task FalseEqualsHasValue()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""              
                  var value = default(int?);
                  _ = false == [|value.HasValue|];
                  """)
              .ShouldFixCodeWith("""
                  var value = default(int?);
                  _ = value is null;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HasValueIsTrue()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""              
                  var value = default(int?);
                  _ = [|value.HasValue|] is true;
                  """)
              .ShouldFixCodeWith("""
                  var value = default(int?);
                  _ = value is not null;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task HasValueIsFalse()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""              
                  var value = default(int?);
                  _ = [|value.HasValue|] is false;
                  """)
              .ShouldFixCodeWith("""
                  var value = default(int?);
                  _ = value is null;
                  """)
              .ValidateAsync();
    }
}

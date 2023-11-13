using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UsePatternMatchingForNullCheckAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithAnalyzer<UsePatternMatchingForNullCheckAnalyzer>()
            .WithCodeFixProvider<UsePatternMatchingForNullCheckFixer>();
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
}

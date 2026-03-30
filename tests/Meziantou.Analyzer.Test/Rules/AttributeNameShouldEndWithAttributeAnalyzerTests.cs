using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AttributeNameShouldEndWithAttributeAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<AttributeNameShouldEndWithAttributeAnalyzer>()
            .WithCodeFixProvider<TypeNameShouldEndWithSuffixFixer>();
    }

    [Fact]
    public async Task NameEndsWithAttribute()
    {
        const string SourceCode = """
            class CustomAttribute : System.Attribute
            {
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NameDoesNotEndWithAttribute()
    {
        const string SourceCode = """
            class [|CustomAttr|] : System.Attribute
            {
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NameDoesNotEndWithAttribute_CodeFix()
    {
        const string SourceCode = """
            class [|CustomAttr|] : System.Attribute
            {
            }
            """;

        const string Fix = """
            class CustomAttrAttribute : System.Attribute
            {
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }
}

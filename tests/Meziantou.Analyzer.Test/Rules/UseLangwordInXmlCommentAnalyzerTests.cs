using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class UseLangwordInXmlCommentAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseLangwordInXmlCommentAnalyzer>()
            .WithCodeFixProvider<UseLangwordInXmlCommentFixer>()
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    [Theory]
    [InlineData("[|<c>void</c>|]", "<see langword=\"void\"/>")]
    [InlineData("[|<code>void</code>|]", "<see langword=\"void\"/>")]
    [InlineData("[|<code>null</code>|]", "<see langword=\"null\"/>")]
    public async Task ValidateSummary_Invalid(string comment, string fix)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
/// <summary>{{comment}}</summary>
class Sample { }
""")
              .ShouldFixCodeWith($$"""
/// <summary>{{fix}}</summary>
class Sample { }
""")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("<i>in</i>")]
    [InlineData("null")]
    [InlineData("this is null")]
    public async Task ValidateSummary_Valid(string comment)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
/// <summary>{{comment}}</summary>
class Sample { }
""")
              .ValidateAsync();
    }
}

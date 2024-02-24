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
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    [Theory]
    [InlineData("[|<c>void</c>|]")]
    [InlineData("[|<code>void</code>|]")]
    [InlineData("[|<code>null</code>|]")]
    [InlineData("<i>in</i>")]
    [InlineData("null")]
    [InlineData("this is null")]
    public async Task ValidateSummary(string comment)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
/// <summary>{{comment}}</summary>
class Sample { }
""")
              .ValidateAsync();
    }
}

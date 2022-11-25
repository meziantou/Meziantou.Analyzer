using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseEventArgsEmptyAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseEventArgsEmptyAnalyzer>()
            .WithCodeFixProvider<UseEventArgsEmptyFixer>();
    }

    [Fact]
    public async Task ShouldReport()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        _ = [||]new System.EventArgs();
    }
}";
        const string Fixed = @"
class TypeName
{
    public void Test()
    {
        _ = System.EventArgs.Empty;
    }
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(Fixed)
            .ValidateAsync();
    }
}

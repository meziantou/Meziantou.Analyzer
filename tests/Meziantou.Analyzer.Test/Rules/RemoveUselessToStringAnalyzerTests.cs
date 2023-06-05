using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RemoveUselessToStringAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<RemoveUselessToStringAnalyzer>()
            .WithCodeFixProvider<RemoveUselessToStringFixer>();
    }

    [Fact]
    public async Task IntToString_ShouldNotReportDiagnostic()
    {
        var project = CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    public void A() => 1.ToString();
}");

        await project.ValidateAsync();
    }

    [Fact]
    public async Task StringToString_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    public string A() => [||]"""".ToString();
}")
              .ShouldFixCodeWith(@"
class Test
{
    public string A() => """";
}")
              .ValidateAsync();
    }
}

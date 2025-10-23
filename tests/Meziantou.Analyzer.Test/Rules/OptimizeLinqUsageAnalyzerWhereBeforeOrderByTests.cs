using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerWhereBeforeOrderByTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: RuleIdentifiers.OptimizeEnumerable_WhereBeforeOrderBy)
            .WithTargetFramework(Helpers.TargetFramework.Net9_0);
    }

    [Theory]
    [InlineData("OrderBy")]
    [InlineData("OrderByDescending")]
    public async Task Enumerable_WhereBeforeOrderBy_Valid(string a)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        System.Collections.Generic.IEnumerable<string> enumerable = null;
        enumerable.Where(x => x != null)." + a + @"(x => x != null);
    }
}
")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("Order")]
    [InlineData("OrderDescending")]
    public async Task Enumerable_WhereBeforeOrder_Valid(string a)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        System.Collections.Generic.IEnumerable<string> enumerable = null;
        enumerable.Where(x => x != null)." + a + @"();
    }
}
")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("OrderBy")]
    [InlineData("OrderByDescending")]
    public async Task Enumerable_WhereAfterOrderBy_Invalid(string a)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        System.Collections.Generic.IEnumerable<string> enumerable = null;
        [||]enumerable." + a + @"(x => x).Where(x => x != null);
    }
}
")
              .ShouldReportDiagnosticWithMessage(message: $"Call 'Where' before '{a}'")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("Order")]
    [InlineData("OrderDescending")]
    public async Task Enumerable_WhereAfterOrder_Invalid(string a)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        System.Collections.Generic.IEnumerable<string> enumerable = null;
        [||]enumerable." + a + @"().Where(x => x != null);
    }
}
")
              .ShouldReportDiagnosticWithMessage(message: $"Call 'Where' before '{a}'")
              .ValidateAsync();
    }
}

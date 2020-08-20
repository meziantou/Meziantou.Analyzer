using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class OptimizeLinqUsageAnalyzerDuplicateOrderByTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: RuleIdentifiers.DuplicateEnumerable_OrderBy)
                .WithCodeFixProvider<OptimizeLinqUsageFixer>();
        }

        [Theory]
        [InlineData("OrderBy", "OrderBy", "ThenBy")]
        [InlineData("OrderBy", "OrderByDescending", "ThenByDescending")]
        [InlineData("OrderByDescending", "OrderBy", "ThenBy")]
        [InlineData("OrderByDescending", "OrderByDescending", "ThenByDescending")]
        public async Task IQueryable_TwoOrderBy_FixRemoveDuplicate(string a, string b, string expectedMethod)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        IQueryable<string> query = null;
        [||]query." + a + @"(x => x)." + b + @"(x => x);
    }
}
")
                  .ShouldReportDiagnosticWithMessage($"Remove the first '{a}' method or use '{expectedMethod}'")
                  .ShouldFixCodeWith(1, @"using System.Linq;
class Test
{
    public Test()
    {
        IQueryable<string> query = null;
        query." + b + @"(x => x);
    }
}
")
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("OrderBy", "OrderBy", "ThenBy")]
        [InlineData("OrderBy", "OrderByDescending", "ThenByDescending")]
        [InlineData("OrderByDescending", "OrderBy", "ThenBy")]
        [InlineData("OrderByDescending", "OrderByDescending", "ThenByDescending")]
        public async Task TwoOrderBy_FixRemoveDuplicate(string a, string b, string expectedMethod)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = Enumerable.Empty<int>();
        [||]enumerable." + a + @"(x => x)." + b + @"(x => x);
    }
}
")
                  .ShouldReportDiagnosticWithMessage($"Remove the first '{a}' method or use '{expectedMethod}'")
                  .ShouldFixCodeWith(1, @"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = Enumerable.Empty<int>();
        enumerable." + b + @"(x => x);
    }
}
")
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("OrderBy", "OrderBy", "ThenBy")]
        [InlineData("OrderBy", "OrderByDescending", "ThenByDescending")]
        [InlineData("OrderByDescending", "OrderBy", "ThenBy")]
        [InlineData("OrderByDescending", "OrderByDescending", "ThenByDescending")]
        public async Task TwoOrderBy_FixWithThenBy(string a, string b, string expectedMethod)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = Enumerable.Empty<int>();
        [||]enumerable." + a + @"(x => x)." + b + @"(x => x);
    }
}
")
                  .ShouldReportDiagnosticWithMessage($"Remove the first '{a}' method or use '{expectedMethod}'")
                  .ShouldFixCodeWith(0, @"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = Enumerable.Empty<int>();
        enumerable." + a + @"(x => x)." + expectedMethod + @"(x => x);
    }
}
")
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("ThenBy", "OrderBy", "ThenBy")]
        [InlineData("ThenByDescending", "OrderBy", "ThenBy")]
        [InlineData("ThenBy", "OrderByDescending", "ThenByDescending")]
        [InlineData("ThenByDescending", "OrderByDescending", "ThenByDescending")]
        public async Task ThenByFollowedByOrderBy(string a, string b, string expectedMethod)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = Enumerable.Empty<int>();
        [||]enumerable.OrderBy(x => x)." + a + @"(x => x)." + b + @"(x => x);
    }
}
")
                  .ShouldReportDiagnosticWithMessage($"Remove the first '{a}' method or use '{expectedMethod}'")
                  .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = Enumerable.Empty<int>();
        enumerable.OrderBy(x => x)." + a + @"(x => x)." + expectedMethod + @"(x => x);
    }
}
")
                  .ValidateAsync();
        }
    }
}

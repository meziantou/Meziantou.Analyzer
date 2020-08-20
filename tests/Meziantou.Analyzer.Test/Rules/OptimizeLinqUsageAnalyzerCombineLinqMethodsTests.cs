using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class OptimizeLinqUsageAnalyzerCombineLinqMethodsTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: RuleIdentifiers.OptimizeLinqUsage)
                .WithCodeFixProvider<OptimizeLinqUsageFixer>();
        }

        [Theory]
        [InlineData("Any")]
        [InlineData("First")]
        [InlineData("FirstOrDefault")]
        [InlineData("Last")]
        [InlineData("LastOrDefault")]
        [InlineData("Single")]
        [InlineData("SingleOrDefault")]
        [InlineData("Count")]
        [InlineData("LongCount")]
        public async Task CombineWhereWithTheFollowingMethod(string methodName)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        [||]enumerable.Where(x => x == 0)." + methodName + @"();
    }
}
")
                  .ShouldReportDiagnosticWithMessage($"Combine 'Where' with '{methodName}'")
                  .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable." + methodName + @"(x => x == 0);
    }
}
")
                  .ValidateAsync();
        }

        [Fact]
        public async Task CombineWhereWithTheFollowingWhereMethod()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        [||]enumerable.Where(x => x == 0).Where(y => true);
    }
}
")
                  .ShouldReportDiagnosticWithMessage($"Combine 'Where' with 'Where'")
                  .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable.Where(x => x == 0 && true);
    }
}
")
                  .ValidateAsync();
        }

        [Fact]
        public async Task CombineWhereWithTheFollowingMethod_CombineLambdaWithNothing()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        [||]enumerable.Where(x => x == 0 || x == 1).Any();
    }
}
")
                  .ShouldReportDiagnosticWithMessage($"Combine 'Where' with 'Any'")
                  .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable.Any(x => x == 0 || x == 1);
    }
}
")
                  .ValidateAsync();
        }

        [Fact]
        public async Task CombineWhereWithTheFollowingMethod_CombineLambdaWithLambda()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        [||]enumerable.Where(x => x == 0 || x == 1).Any(y => y == 2);
    }
}
")
                  .ShouldReportDiagnosticWithMessage($"Combine 'Where' with 'Any'")
                  .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable.Any(x => (x == 0 || x == 1) && x == 2);
    }
}
")
                  .ValidateAsync();
        }

        [Fact]
        public async Task CombineWhereWithTheFollowingMethod_CombineMethodGroupWithNothing()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        [||]enumerable.Where(Filter).Any();
    }

    bool Filter(int x) => true;
}
")
                  .ShouldReportDiagnosticWithMessage("Combine 'Where' with 'Any'")
                  .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable.Any(Filter);
    }

    bool Filter(int x) => true;
}
")
                  .ValidateAsync();
        }

        [Fact]
        public async Task CombineWhereWithTheFollowingMethod_CombineMethodGroupWithMethodGroup()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        [||]enumerable.Where(Filter).Any(Filter);
    }

    bool Filter(int x) => true;
}
")
                  .ShouldReportDiagnosticWithMessage("Combine 'Where' with 'Any'")
                  .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable.Any(x => Filter(x) && Filter(x));
    }

    bool Filter(int x) => true;
}
")
                  .ValidateAsync();
        }

        [Fact]
        public async Task CombineWhereWithTheFollowingMethod_CombineMethodGroupWithLambda()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        [||]enumerable.Where(Filter).Any(x => true);
    }

    bool Filter(int x) => true;
}
")
                  .ShouldReportDiagnosticWithMessage("Combine 'Where' with 'Any'")
                  .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable.Any(x => Filter(x) && true);
    }

    bool Filter(int x) => true;
}
")
                  .ValidateAsync();
        }

        [Fact]
        public async Task CombineWhereWithAny_DoNotReportForWhereWithIndex()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable.Where(Filter).Any(x => true);
    }

    bool Filter(int x, int index) => true;
}
")
                  .ValidateAsync();
        }
    }
}

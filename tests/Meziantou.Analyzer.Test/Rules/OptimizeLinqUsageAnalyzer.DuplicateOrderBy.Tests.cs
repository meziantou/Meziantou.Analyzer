using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class OptimizeLinqUsageAnalyzerDuplicateOrderByTests
    {
        // TODO test IQueryable
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: "MA0030")
                .WithCodeFixProvider<OptimizeLinqUsageFixer>();
        }

        [DataTestMethod]
        [DataRow("OrderBy", "OrderBy", "ThenBy")]
        [DataRow("OrderBy", "OrderByDescending", "ThenByDescending")]
        [DataRow("OrderByDescending", "OrderBy", "ThenBy")]
        [DataRow("OrderByDescending", "OrderByDescending", "ThenByDescending")]
        public async Task TwoOrderBy_FixRemoveDuplicate(string a, string b, string expectedMethod)
        {
            await CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable." + a + @"(x => x)." + b + @"(x => x);
    }
}
")
                  .ShouldReportDiagnostic(line: 7, column: 9, message: $"Remove the first '{a}' method or use '{expectedMethod}'")
                  .ShouldFixCodeWith(1, @"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable." + b + @"(x => x);
    }
}
")
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("OrderBy", "OrderBy", "ThenBy")]
        [DataRow("OrderBy", "OrderByDescending", "ThenByDescending")]
        [DataRow("OrderByDescending", "OrderBy", "ThenBy")]
        [DataRow("OrderByDescending", "OrderByDescending", "ThenByDescending")]
        public async Task TwoOrderBy_FixWithThenBy(string a, string b, string expectedMethod)
        {
            await CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable." + a + @"(x => x)." + b + @"(x => x);
    }
}
")
                  .ShouldReportDiagnostic(line: 7, column: 9, message: $"Remove the first '{a}' method or use '{expectedMethod}'")
                  .ShouldFixCodeWith(0, @"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable." + a + @"(x => x)." + expectedMethod + @"(x => x);
    }
}
")
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("ThenBy", "OrderBy", "ThenBy")]
        [DataRow("ThenByDescending", "OrderBy", "ThenBy")]
        [DataRow("ThenBy", "OrderByDescending", "ThenByDescending")]
        [DataRow("ThenByDescending", "OrderByDescending", "ThenByDescending")]
        public async Task ThenByFollowedByOrderBy(string a, string b, string expectedMethod)
        {
            await CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable.OrderBy(x => x)." + a + @"(x => x)." + b + @"(x => x);
    }
}
")
                  .ShouldReportDiagnostic(line: 7, column: 9, message: $"Remove the first '{a}' method or use '{expectedMethod}'")
                  .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable.OrderBy(x => x)." + a + @"(x => x)." + expectedMethod + @"(x => x);
    }
}
")
                  .ValidateAsync();
        }
    }
}

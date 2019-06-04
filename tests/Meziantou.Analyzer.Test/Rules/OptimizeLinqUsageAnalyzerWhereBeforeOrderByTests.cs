using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class OptimizeLinqUsageAnalyzerWhereBeforeOrderByTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: "MA0063");
        }

        [DataTestMethod]
        [DataRow("OrderBy")]
        [DataRow("OrderByDescending")]
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

        [DataTestMethod]
        [DataRow("OrderBy")]
        [DataRow("OrderByDescending")]
        public async Task Enumerable_WhereAfterOrderBy_Invalid(string a)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        System.Collections.Generic.IEnumerable<string> enumerable = null;
        [|]enumerable." + a + @"(x => x).Where(x => x != null);
    }
}
")
                  .ShouldReportDiagnosticWithMessage(message: $"Call 'Where' before '{a}'")
                  .ValidateAsync();
        }
    }
}

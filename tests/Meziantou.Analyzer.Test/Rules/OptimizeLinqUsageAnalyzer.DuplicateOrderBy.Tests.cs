using System.Collections.Generic;
using System.Linq;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class OptimizeLinqUsageAnalyzerDuplicateOrderByTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: "MA0030");
        }

        [DataTestMethod]
        [DataRow("OrderBy", "OrderBy", "ThenBy")]
        [DataRow("OrderBy", "OrderByDescending", "ThenByDescending")]
        [DataRow("OrderByDescending", "OrderBy", "ThenBy")]
        [DataRow("OrderByDescending", "OrderByDescending", "ThenByDescending")]
        public async System.Threading.Tasks.Task TwoOrderByAsync(string a, string b, string expectedMethod)
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
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("ThenBy", "OrderBy", "ThenBy")]
        [DataRow("ThenByDescending", "OrderBy", "ThenBy")]
        [DataRow("ThenBy", "OrderByDescending", "ThenByDescending")]
        [DataRow("ThenByDescending", "OrderByDescending", "ThenByDescending")]
        public async System.Threading.Tasks.Task ThenByFollowedByOrderByAsync(string a, string b, string expectedMethod)
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
                  .ValidateAsync();
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class OptimizeLinqUsageAnalyzerCombineLinqMethodsTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: "MA0029");
        }

        [DataTestMethod]
        [DataRow("Any", null)]
        [DataRow("First", null)]
        [DataRow("FirstOrDefault", null)]
        [DataRow("Last", null)]
        [DataRow("LastOrDefault", null)]
        [DataRow("Single", null)]
        [DataRow("SingleOrDefault", null)]
        [DataRow("Count", null)]
        [DataRow("LongCount", null)]
        [DataRow("Where", "x => true")]
        public async System.Threading.Tasks.Task CombineWhereWithTheFollowingMethodAsync(string methodName, string arguments)
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
        enumerable.Where(x => true)." + methodName + @"(" + arguments + @");
    }
}
")
                  .ShouldReportDiagnostic(line: 7, column: 9, message: $"Combine 'Where' with '{methodName}'")
                  .ValidateAsync();
        }
    }
}

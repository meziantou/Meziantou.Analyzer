using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class OptimizeLinqUsageAnalyzerCountTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: "MA0031");
        }

        [DataTestMethod]
        [DataRow("Count() == -1", "Expression is always false")]
        [DataRow("Count() == 0", "Replace 'Count() == 0' with 'Any() == false'")]
        [DataRow("Count() == 1", "Replace 'Count() == 1' with 'Take(2).Count() == 1'")]
        [DataRow("Take(10).Count() == 1", null)]
        public async Task Count_Equals(string text, string expectedMessage)
        {
            var project = CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
");

            if (expectedMessage == null)
            {
                project.ShouldNotReportDiagnostic();
            }
            else
            {
                project.ShouldReportDiagnostic(line: 7, column: 13, message: expectedMessage);
            }

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Count() != -2", "Expression is always true")]
        [DataRow("Count() != 0", "Replace 'Count() != 0' with 'Any()'")]
        [DataRow("Count() != 10", "Replace 'Count() != 10' with 'Take(11).Count() != 10'")]
        [DataRow("Count() != n", "Replace 'Count() != n' with 'Take(n + 1).Count() != n'")]
        [DataRow("Take(1).Count() != n", null)]
        public async Task Count_NotEquals(string text, string expectedMessage)
        {
            var project = CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        int n = 10;
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
");

            if (expectedMessage == null)
            {
                project.ShouldNotReportDiagnostic();
            }
            else
            {
                project.ShouldReportDiagnostic(line: 8, column: 13, message: expectedMessage);
            }

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Count() < -1", "Expression is always false")]
        [DataRow("Count() < 0", "Expression is always false")]
        [DataRow("Count() < 1", "Replace 'Count() < 1' with 'Any() == false'")]
        [DataRow("Count() < 2", "Replace 'Count() < 2' with 'Skip(1).Any() == false'")]
        [DataRow("Count() < n", "Replace 'Count() < n' with 'Skip(n - 1).Any() == false'")]
        public async Task Count_LessThan(string text, string expectedMessage)
        {
            await CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        int n = 10;
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
")
                  .ShouldReportDiagnostic(line: 8, column: 13, message: expectedMessage)
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Count() <= -1", "Expression is always false")]
        [DataRow("Count() <= 0", "Replace 'Count() <= 0' with 'Any() == false'")]
        [DataRow("Count() <= 1", "Replace 'Count() <= 1' with 'Skip(1).Any() == false'")]
        [DataRow("Count() <= 2", "Replace 'Count() <= 2' with 'Skip(2).Any() == false'")]
        [DataRow("Count() <= n", "Replace 'Count() <= n' with 'Skip(n).Any() == false'")]
        public async Task Count_LessThanOrEqual(string text, string expectedMessage)
        {
            await CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        int n = 10;
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
")
                  .ShouldReportDiagnostic(line: 8, column: 13, message: expectedMessage)
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Count() > -1", "Expression is always true")]
        [DataRow("Count() > 0", "Replace 'Count() > 0' with 'Any()'")]
        [DataRow("Count() > 1", "Replace 'Count() > 1' with 'Skip(1).Any()'")]
        [DataRow("Count() > 2", "Replace 'Count() > 2' with 'Skip(2).Any()'")]
        [DataRow("Count() > n", "Replace 'Count() > n' with 'Skip(n).Any()'")]
        public async Task Count_GreaterThan(string text, string expectedMessage)
        {
            await CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        int n = 10;
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
")
                  .ShouldReportDiagnostic(line: 8, column: 13, message: expectedMessage)
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("enumerable.Count() >= -1", "Expression is always true")]
        [DataRow("-1 <= enumerable.Count()", "Expression is always true")]
        [DataRow("enumerable.Count() >= 0", "Expression is always true")]
        [DataRow("enumerable.Count() >= 1", "Replace 'Count() >= 1' with 'Any()'")]
        [DataRow("enumerable.Count() >= 2", "Replace 'Count() >= 2' with 'Skip(1).Any()'")]
        [DataRow("enumerable.Count() >= n", "Replace 'Count() >= n' with 'Skip(n - 1).Any()'")]
        public async Task Count_GreaterThanOrEqual(string text, string expectedMessage)
        {
            await CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        int n = 10;
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = " + text + @";
    }
}
")
                  .ShouldReportDiagnostic(line: 8, column: 13, message: expectedMessage)
                  .ValidateAsync();
        }
    }
}

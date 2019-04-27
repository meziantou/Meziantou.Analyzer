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
                .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: "MA0031")
                .WithCodeFixProvider<OptimizeLinqUsageFixer>();
        }

        [DataTestMethod]
        [DataRow("enumerable.Count() < 0")]
        [DataRow("enumerable.Count() <= -1")]
        [DataRow("enumerable.Count() <= -2")]
        [DataRow("enumerable.Count() == -1")]
        [DataRow("-1 == enumerable.Count()")]
        public async Task Count_AlwaysFalse(string text)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = Enumerable.Empty<int>();
        _ = " + text + @";
    }
}
")
                .ShouldReportDiagnostic(line: 7, column: 13, message: "Expression is always false")
                .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = Enumerable.Empty<int>();
        _ = false;
    }
}
")
                .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("enumerable.Count() != -2")]
        [DataRow("enumerable.Count() > -1")]
        [DataRow("enumerable.Count() >= 0")]
        [DataRow("-10 <= enumerable.Count()")]
        public async Task Count_AlwaysTrue(string text)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        int n = 10;
        var enumerable = Enumerable.Empty<int>();
        _ = " + text + @";
    }
}
")
                  .ShouldReportDiagnostic(line: 8, column: 13, message: "Expression is always true")
                  .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        int n = 10;
        var enumerable = Enumerable.Empty<int>();
        _ = true;
    }
}
")
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Count() == 0", "Replace 'Count() == 0' with 'Any() == false'")]
        [DataRow("Count() < 1", "Replace 'Count() < 1' with 'Any() == false'")]
        [DataRow("Count() <= 0", "Replace 'Count() <= 0' with 'Any() == false'")]
        public async Task Count_AnyFalse(string text, string expectedMessage)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
")
                   .ShouldReportDiagnostic(line: 7, column: 13, message: expectedMessage)
                   .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = !enumerable.Any();
    }
}
")
                   .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Count() != 0", "Replace 'Count() != 0' with 'Any()'")]
        [DataRow("Count() > 0", "Replace 'Count() > 0' with 'Any()'")]
        [DataRow("Count() >= 1", "Replace 'Count() >= 1' with 'Any()'")]
        public async Task Count_AnyTrue(string text, string expectedMessage)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
")
                   .ShouldReportDiagnostic(line: 7, column: 13, message: expectedMessage)
                   .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable.Any();
    }
}
")
                   .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Count() == 1", "Take(2).Count() == 1", "Replace 'Count() == 1' with 'Take(2).Count() == 1'")]
        [DataRow("Count() != 10", "Take(11).Count() != 10", "Replace 'Count() != 10' with 'Take(11).Count() != 10'")]
        [DataRow("Count() != n", "Take(n + 1).Count() != n", "Replace 'Count() != n' with 'Take(n + 1).Count() != n'")]
        [DataRow("Count(x => x > 1) != n", "Where(x => x > 1).Take(n + 1).Count() != n", "Replace 'Count() != n' with 'Take(n + 1).Count() != n'")]
        public async Task Count_TakeAndCount(string text, string fix, string expectedMessage)
        {
            await CreateProjectBuilder()
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
                   .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        int n = 10;
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable." + fix + @";
    }
}
")
                   .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Count() > 1", "Skip(1).Any()", "Replace 'Count() > 1' with 'Skip(1).Any()'")]
        [DataRow("Count() > 2", "Skip(2).Any()", "Replace 'Count() > 2' with 'Skip(2).Any()'")]
        [DataRow("Count() > n", "Skip(n).Any()", "Replace 'Count() > n' with 'Skip(n).Any()'")]
        [DataRow("Count() >= 2", "Skip(1).Any()", "Replace 'Count() >= 2' with 'Skip(1).Any()'")]
        [DataRow("Count() >= n", "Skip(n - 1).Any()", "Replace 'Count() >= n' with 'Skip(n - 1).Any()'")]
        public async Task Count_SkipAndAny(string text, string fix, string expectedMessage)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        int n = 10;
        var enumerable = Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
")
                   .ShouldReportDiagnostic(line: 8, column: 13, message: expectedMessage)
                   .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        int n = 10;
        var enumerable = Enumerable.Empty<int>();
        _ = enumerable." + fix + @";
    }
}
")
                   .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Count() < 2", "Skip(1).Any()", "Replace 'Count() < 2' with 'Skip(1).Any() == false'")]
        [DataRow("Count() < n", "Skip(n - 1).Any()", "Replace 'Count() < n' with 'Skip(n - 1).Any() == false'")]
        [DataRow("Count() <= 1", "Skip(1).Any()", "Replace 'Count() <= 1' with 'Skip(1).Any() == false'")]
        [DataRow("Count() <= 2", "Skip(2).Any()", "Replace 'Count() <= 2' with 'Skip(2).Any() == false'")]
        [DataRow("Count() <= n", "Skip(n).Any()", "Replace 'Count() <= n' with 'Skip(n).Any() == false'")]
        [DataRow("Count(x => true) <= n", "Where(x => true).Skip(n).Any()", "Replace 'Count() <= n' with 'Skip(n).Any() == false'")]
        public async Task Count_NotSkipAndAny(string text, string fix, string expectedMessage)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        int n = 10;
        var enumerable = Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
")
                   .ShouldReportDiagnostic(line: 8, column: 13, message: expectedMessage)
                   .ShouldFixCodeWith(@"using System.Linq;
class Test
{
    public Test()
    {
        int n = 10;
        var enumerable = Enumerable.Empty<int>();
        _ = !enumerable." + fix + @";
    }
}
")
                   .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Take(10).Count() == 1", null)]
        public async Task Count_Equals(string text, string expectedMessage)
        {
            var project = CreateProjectBuilder()
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

            if (expectedMessage != null)
            {
                project.ShouldReportDiagnostic(line: 7, column: 13, message: expectedMessage);
            }

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Take(1).Count() != n", null)]
        public async Task Count_NotEquals(string text, string expectedMessage)
        {
            var project = CreateProjectBuilder()
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

            if (expectedMessage != null)
            {
                project.ShouldReportDiagnostic(line: 8, column: 13, message: expectedMessage);
            }

            await project.ValidateAsync();
        }
    }
}

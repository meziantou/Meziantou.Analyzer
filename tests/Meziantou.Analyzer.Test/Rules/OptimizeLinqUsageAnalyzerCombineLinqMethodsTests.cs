﻿using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class OptimizeLinqUsageAnalyzerCombineLinqMethodsTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: "MA0029")
                .WithCodeFixProvider<OptimizeLinqUsageFixer>();
        }

        [DataTestMethod]
        [DataRow("Any")]
        [DataRow("First")]
        [DataRow("FirstOrDefault")]
        [DataRow("Last")]
        [DataRow("LastOrDefault")]
        [DataRow("Single")]
        [DataRow("SingleOrDefault")]
        [DataRow("Count")]
        [DataRow("LongCount")]
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

        [TestMethod]
        [DataRow("Where", "x => true")]
        public async Task CombineWhereWithTheFollowingMethod()
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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
    }
}

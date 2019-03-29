using System.Collections.Generic;
using System.Linq;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class OptimizeLinqUsageAnalyzerDuplicateOrderByTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new OptimizeLinqUsageAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0030";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Info;

        [DataTestMethod]
        [DataRow("OrderBy", "OrderBy", "ThenBy")]
        [DataRow("OrderBy", "OrderByDescending", "ThenByDescending")]
        [DataRow("OrderByDescending", "OrderBy", "ThenBy")]
        [DataRow("OrderByDescending", "OrderByDescending", "ThenByDescending")]
        public void TwoOrderBy(string a, string b, string expectedMethod)
        {
            var project = new ProjectBuilder()
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
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 9, message: $"Remove the first '{a}' method or use '{expectedMethod}'"));
        }

        [DataTestMethod]
        [DataRow("ThenBy", "OrderBy", "ThenBy")]
        [DataRow("ThenByDescending", "OrderBy", "ThenBy")]
        [DataRow("ThenBy", "OrderByDescending", "ThenByDescending")]
        [DataRow("ThenByDescending", "OrderByDescending", "ThenByDescending")]
        public void ThenByFollowedByOrderBy(string a, string b, string expectedMethod)
        {
            var project = new ProjectBuilder()
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
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 9, message: $"Remove the first '{a}' method or use '{expectedMethod}'"));
        }
    }
}

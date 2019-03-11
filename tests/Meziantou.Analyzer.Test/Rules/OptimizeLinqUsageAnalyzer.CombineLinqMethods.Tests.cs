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
    public class OptimizeLinqUsageAnalyzerCombineLinqMethodsTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new OptimizeLinqUsageAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0029";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Info;

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
        public void CombineWhereWithTheFollowingMethod(string methodName, string arguments)
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSource(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable.Where(x => true)." + methodName + @"(" + arguments + @");
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 9, message: $"Combine 'Where' with '{methodName}'"));
        }
    }
}

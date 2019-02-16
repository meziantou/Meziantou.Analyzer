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
    public class UseListOfTMethodsInsteadOfEnumerableExtensionMethodsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseListOfTMethodsInsteadOfEnumerableExtensionMethodsAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0020";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Info;

        [TestMethod]
        public void FirstOrDefault()
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
        var list = new System.Collections.Generic.List<int>();
        list.FirstOrDefault();
        list.FirstOrDefault(x => x == 0); // Error
        enumerable.FirstOrDefault();
        enumerable.FirstOrDefault(x => x == 0);
    }
}
");

            var expected = new[]
            {
                CreateDiagnosticResult(line: 9, column: 9, message: "Use 'Find' instead of 'FirstOrDefault'"),
            };
            VerifyDiagnostic(project, expected);
        }
    }
}

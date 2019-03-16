using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DontUseInstanceFieldsOfTypeAsyncLocalAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new DontUseInstanceFieldsOfTypeAsyncLocalAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0034";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void DontReport()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test2
{
    int _a;
    static System.Threading.AsyncLocal<int> _b;
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Report()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test2
{
    System.Threading.AsyncLocal<int> _a;
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 38));
        }
    }
}

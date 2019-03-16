using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DontTagInstanceFieldsWithThreadStaticAttributeAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new DontTagInstanceFieldsWithThreadStaticAttributeAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0033";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void DontReport()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test2
{
    int _a;
    [System.ThreadStatic]
    static int _b;
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
    [System.ThreadStatic]
    int _a;
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 5, column: 9));
        }
    }
}

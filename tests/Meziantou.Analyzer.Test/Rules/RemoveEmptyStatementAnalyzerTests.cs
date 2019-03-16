using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class RemoveEmptyStatementAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new RemoveEmptyStatementAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0037";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Error;

        [TestMethod]
        public void EmptyStatement()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    public void A()
    {
        ;
    }
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 9));
        }

        [TestMethod]
        public void EmptyStatementInALabel()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    public void A()
    {
test:
        ;
    }
}");

            VerifyDiagnostic(project);
        }
    }
}

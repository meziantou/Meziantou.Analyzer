using System.Threading;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DontUseDangerousThreadingMethodsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new DontUseDangerousThreadingMethodsAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0035";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [DataTestMethod]
        [DataRow("Thread.CurrentThread.Abort()")]
        [DataRow("Thread.CurrentThread.Suspend()")]
        [DataRow("Thread.CurrentThread.Resume()")]
        public void ReportDiagnostic(string text)
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Thread))
                  .WithSourceCode(@"using System.Threading;
public class Test
{
    public void A()
    {
        " + text + @";
    }
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 9));
        }
    }
}

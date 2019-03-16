using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class TypesShouldNotExtendSystemApplicationExceptionAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new TypesShouldNotExtendSystemApplicationExceptionAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0013";
        protected override string ExpectedDiagnosticMessage => "Types should not extend System.ApplicationException";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void InheritFromException_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .WithSource("class Test : System.Exception { }");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void InheritFromApplicationException_ShouldReportError()
        {
            var project = new ProjectBuilder()
                  .WithSource("class Test : System.ApplicationException { }");

            var expected = CreateDiagnosticResult(line: 1, column: 7);
            VerifyDiagnostic(project, expected);
        }
    }
}

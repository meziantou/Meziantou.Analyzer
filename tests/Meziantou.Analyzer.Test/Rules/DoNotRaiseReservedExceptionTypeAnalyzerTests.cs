using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DoNotRaiseReservedExceptionTypeAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new DoNotRaiseReservedExceptionTypeAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0012";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void RaiseNotReservedException_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System;
class TestAttribute
{
    void Test()
    {
        throw new Exception();
        throw new ArgumentException();
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void RaiseReservedException_ShouldReportError()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System;
class TestAttribute
{
    void Test()
    {
        throw new IndexOutOfRangeException();
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 9, message: "'System.IndexOutOfRangeException' is a reserved exception type");
            VerifyDiagnostic(project, expected);
        }
    }
}

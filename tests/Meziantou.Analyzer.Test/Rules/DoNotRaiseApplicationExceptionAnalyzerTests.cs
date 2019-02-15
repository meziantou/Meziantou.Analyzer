using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DoNotRaiseApplicationExceptionAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new DoNotRaiseApplicationExceptionAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0014";
        protected override string ExpectedDiagnosticMessage => "Do not raise System.ApplicationException type";
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

        try
        {
        }
        catch (ApplicationException)
        {
            throw;
        }
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
        throw new ApplicationException();
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 9);
            VerifyDiagnostic(project, expected);
        }
    }
}

using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DoNotRaiseNotImplementedExceptionAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new DoNotRaiseNotImplementedExceptionAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0025";
        protected override string ExpectedDiagnosticMessage => "Do not raise System.NotImplementedException";
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

        try
        {
        }
        catch (NotImplementedException)
        {
            throw;
        }
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void RaiseNotImplementedException_ShouldReportError()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System;
class TestAttribute
{
    void Test()
    {
        throw new NotImplementedException();
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 9);
            VerifyDiagnostic(project, expected);
        }
    }
}

using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DoNotRemoveOriginalExceptionFromThrowStatementAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new DoNotRemoveOriginalExceptionFromThrowStatementAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new DoNotRemoveOriginalExceptionFromThrowStatementFixer();
        protected override string ExpectedDiagnosticId => "MA0027";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    internal void Sample()
    {
        throw new System.Exception();

        try
        {
            throw new System.Exception();
        }
        catch (System.Exception ex)
        {
            throw new System.Exception(""test"", ex);
        }
    }
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    internal void Sample()
    {
        try
        {
        }
        catch (System.Exception ex)
        {
            _ = ex;
            throw ex;
        }
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 12, column: 13));
            VerifyFix(project, @"
class Test
{
    internal void Sample()
    {
        try
        {
        }
        catch (System.Exception ex)
        {
            _ = ex;
            throw;
        }
    }
}
");
        }
    }
}

using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class UseAnOverloadThatHaveCancellationTokenAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseAnOverloadThatHaveCancellationTokenAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0032";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Info;

        [TestMethod]
        public void CallingMethodWithoutCancellationToken_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    public void A()
    {
        MethodWithCancellationToken();
    }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 9));
        }

        [TestMethod]
        public void CallingMethodWithCancellationToken_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    public void A()
    {
        MethodWithCancellationToken(default);
    }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void CallingMethodWithCancellationToken_ShouldReportDiagnosticWithParameterName()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    public void A(System.Threading.CancellationToken cancellationToken)
    {
        MethodWithCancellationToken();
    }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 9, message: "Specify a CancellationToken (cancellationToken)"));
        }

        [TestMethod]
        public void CallingMethodWithObjectThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    public void A(HttpRequest request)
    {
        MethodWithCancellationToken();
    }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}

class HttpRequest
{
    public System.Threading.CancellationToken RequestAborted { get; }
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 9, message: "Specify a CancellationToken (request.RequestAborted)"));
        }
    }
}

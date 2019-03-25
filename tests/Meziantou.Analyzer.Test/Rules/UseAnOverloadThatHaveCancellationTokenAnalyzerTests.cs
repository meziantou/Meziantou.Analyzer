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

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 9, id: "MA0032"));
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

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 9, id: "MA0040", message: "Specify a CancellationToken (cancellationToken)"));
        }

        [TestMethod]
        public void CallingMethodWithObjectThatContainsAPropertyOfTypeCancellationToken_ShouldReportDiagnosticWithParameterName()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    public static void A(HttpRequest request)
    {
        MethodWithCancellationToken();
    }

    public static void MethodWithCancellationToken() => throw null;
    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}

class HttpRequest
{
    public System.Threading.CancellationToken RequestAborted { get; }
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 9, id: "MA0040", message: "Specify a CancellationToken (request.RequestAborted)"));
        }

        [TestMethod]
        public void CallingMethodWithProperty_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    public void A()
    {
        MethodWithCancellationToken();
    }

    public System.Threading.CancellationToken MyCancellationToken { get; }
    public HttpRequest Request { get; }

    public void MethodWithCancellationToken() => throw null;
    public void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}

class HttpRequest
{
    public System.Threading.CancellationToken RequestAborted { get; }
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 9, id: "MA0040", message: "Specify a CancellationToken (MyCancellationToken, Request.RequestAborted)"));
        }

        [TestMethod]
        public void CallingMethodWithInstanceProperty_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    public static void A()
    {
        MethodWithCancellationToken();
    }

    public static System.Threading.CancellationToken MyCancellationToken { get; }
    public HttpRequest Request { get; }

    public static void MethodWithCancellationToken() => throw null;
    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}

class HttpRequest
{
    public System.Threading.CancellationToken RequestAborted { get; }
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 9, id: "MA0040", message: "Specify a CancellationToken (MyCancellationToken)"));
        }

        [TestMethod]
        public void CallingMethod_ShouldReportDiagnosticWithVariables()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    public static void A()
    {
        {
            System.Threading.CancellationToken unaccessible1 = default;
        }

        System.Threading.CancellationToken a = default;
        MethodWithCancellationToken();
        System.Threading.CancellationToken unaccessible2 = default;
    }

    public static void MethodWithCancellationToken() => throw null;
    public static void MethodWithCancellationToken(System.Threading.CancellationToken cancellationToken) => throw null;
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 11, column: 9, id: "MA0040", message: "Specify a CancellationToken (a)"));
        }
    }
}

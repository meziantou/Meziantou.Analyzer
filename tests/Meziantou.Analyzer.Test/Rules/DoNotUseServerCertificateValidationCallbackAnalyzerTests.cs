using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DoNotUseServerCertificateValidationCallbackAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new DoNotUseServerCertificateValidationCallbackAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0039";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Error;

        [TestMethod]
        public void StaticMembers()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    void A()
    {
        System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certification, chain, sslPolicyErrors) => throw null;
    }
}

namespace System.Net
{
    public class ServicePointManager
    {
        public static System.Net.Security.RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }
    }
}

namespace System.Net.Security
{
    public delegate bool RemoteCertificateValidationCallback(object sender, object certificate, object chain, object sslPolicyErrors);
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 9));
        }
    }
}

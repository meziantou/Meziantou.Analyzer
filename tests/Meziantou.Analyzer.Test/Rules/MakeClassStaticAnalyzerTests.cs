using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class MakeClassStaticAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new MakeClassStaticAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0032";
        protected override string ExpectedDiagnosticMessage => "Make class static";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Info;

        [TestMethod]
        public void NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
abstract class Test
{
    static void A() { }
}

class Test2
{
    static void A() { }
}

class Test : Test2 { }");

            VerifyDiagnostic(project);
        }
    }
}

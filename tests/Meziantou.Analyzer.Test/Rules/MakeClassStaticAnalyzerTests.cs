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
        protected override string ExpectedDiagnosticId => "MA0036";
        protected override string ExpectedDiagnosticMessage => "Make class static";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Info;

        [TestMethod]
        public void AbstractClass_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
abstract class AbstractClass
{
    static void A() { }
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Inherited_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    static void A() { }
}

class Test2 : Test { }
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void InstanceField_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test4
{
    int _a;
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void ImplementInterface_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test : ITest
{
}

interface ITest { }
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Diagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    const int a = 10;
    static void A() { }
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 2, column: 7));
        }
    }
}

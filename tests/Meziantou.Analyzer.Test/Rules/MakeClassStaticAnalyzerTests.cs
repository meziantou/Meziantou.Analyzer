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
        public void NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
// The class is abstract
abstract class AbstractClass
{
    static void A() { }
}

// Test3 inherits from the class
class Test2
{
    static void A() { }
}

class Test3 : Test2 { }

// The class has one instance field
class Test4
{
    int _a;
}
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
    static void A() { }
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 2, column: 7));
        }
    }
}

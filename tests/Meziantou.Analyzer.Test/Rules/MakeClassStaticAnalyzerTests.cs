using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class MakeClassStaticAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new MakeClassStaticAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new MakeClassStaticFixer();
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
        public void StaticMethodAndConstField_Diagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
public class Test
{
    const int a = 10;
    static void A() { }
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 2, column: 14));

            var fix = @"
public static class Test
{
    const int a = 10;
    static void A() { }
}";
            VerifyFix(project, fix);
        }

        [TestMethod]
        public void ConversionOperator_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    public static implicit operator int(Test _) => 1;
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AddOperator_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    public static Test operator +(Test a, Test b) => throw null;
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void ComImport_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
[System.Runtime.InteropServices.CoClass(typeof(Test))]
interface ITest
{
}

class Test
{
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Instantiation_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class Test
{
    public static void A() => new Test();
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MsTestClass_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddMSTest()
                  .WithSource(@"
[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
class Test
{
}
");

            VerifyDiagnostic(project);
        }
    }
}

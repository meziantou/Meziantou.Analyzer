using System.Collections.Generic;
using System.Linq;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class MakeMethodStaticAnalyzerTests_Properties : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new MakeMethodStaticAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0041";
        protected override string ExpectedDiagnosticMessage => "Make property static";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Info;

        [TestMethod]
        public void ExpressionBody()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TestClass
{
    int A => throw null;
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 9));
        }

        [TestMethod]
        public void AccessInstanceProperty_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TestClass
{
    int A => TestProperty;

    public int TestProperty { get; }
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AccessInstanceMethodInLinqQuery_Where_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TestClass
{
    int A { get; set; }
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AccessStaticProperty()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TestClass
{
    int A => TestProperty;

    public static int TestProperty => 0;
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 9));
        }

        [TestMethod]
        public void AccessStaticMethod()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TestClass
{
    int A => TestMethod();

    public static int TestMethod() => 0;
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 9));
        }

        [TestMethod]
        public void AccessStaticField()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TestClass
{
    int A => _a;

    static int _a;
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 9));
        }

        [TestMethod]
        public void AccessInstanceField()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TestClass
{
    int A => _a;

    public int _a;
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MethodImplementAnInterface()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TestClass : ITest
{
    public int A { get; }
}

interface ITest
{
    int A { get; }
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MethodExplicitlyImplementAnInterface()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TestClass : ITest
{
    int ITest.A { get; }
}

interface ITest
{
    int A { get; }
}
");

            VerifyDiagnostic(project);
        }
    }
}

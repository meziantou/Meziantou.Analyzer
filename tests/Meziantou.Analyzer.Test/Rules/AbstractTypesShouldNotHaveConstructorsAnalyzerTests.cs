using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class AbstractTypesShouldNotHaveConstructorsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new AbstractTypesShouldNotHaveConstructorsAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new AbstractTypesShouldNotHaveConstructorsFixer();
        protected override string ExpectedDiagnosticId => "MA0017";
        protected override string ExpectedDiagnosticMessage => "Abstract types should not have public or internal constructors";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void Ctor()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
abstract class Test
{
    protected Test(int a) { }
    private Test(object a) { }
}

class Test2
{
    public Test2() { }
    internal Test2(long a) { }
    protected Test2(int a) { }
    private Test2(object a) { }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void PublicCtor()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
abstract class Test
{
    public Test() { }
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 5));
            VerifyFix(project, @"
abstract class Test
{
    protected Test() { }
}");
        }

        [TestMethod]
        public void InternalCtor()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
abstract class Test
{
    internal Test() { }
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 5));
            VerifyFix(project, @"
abstract class Test
{
    protected Test() { }
}");
        }
    }
}

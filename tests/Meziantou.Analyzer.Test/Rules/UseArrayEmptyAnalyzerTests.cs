using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class UseArrayEmptyAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseArrayEmptyAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new UseArrayEmptyFixer();
        protected override string ExpectedDiagnosticId => "MA0005";
        protected override string ExpectedDiagnosticMessage => "Use Array.Empty<T>()";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void EmptyString_ShouldNotReportDiagnosticForEmptyString()
        {
            var project = new ProjectBuilder();
            VerifyDiagnostic(project);
        }

        [DataTestMethod]
        [DataRow("new int[0]")]
        [DataRow("new int[] { }")]
        public void EmptyArray_ShouldReportError(string code)
        {
            var project = new ProjectBuilder()
                  .WithSource($@"
class TestClass
{{
    void Test()
    {{
        var a = {code};
    }}
}}");

            var expected = CreateDiagnosticResult(line: 6, column: 17);
            VerifyDiagnostic(project, expected);

            var fixtest = @"
class TestClass
{
    void Test()
    {
        var a = System.Array.Empty<int>();
    }
}";
            VerifyFix(project, fixtest);
        }

        [DataTestMethod]
        [DataRow("new int[1]")]
        [DataRow("new int[] { 0 }")]
        public void NonEmptyArray_ShouldReportError(string code)
        {
            var project = new ProjectBuilder()
                  .WithSource($@"
class TestClass
{{
    void Test()
    {{
        var a = {code};
    }}
}}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Length_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TestClass
{
    void Test()
    {
        int length = 0;
        var a = new int[length];
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void ParamsMethod_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
public class TestClass
{
    public void Test(params string[] values)
    {
    }

    public void CallTest()
    {
        Test();
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void EmptyArrayInAttribute_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
[Test(new int[0])]
class TestAttribute : System.Attribute
{
    public TestAttribute(int[] data) { }
}");

            VerifyDiagnostic(project);
        }
    }
}

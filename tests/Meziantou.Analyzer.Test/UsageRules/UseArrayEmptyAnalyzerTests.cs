using Meziantou.Analyzer.UsageRules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.UsageRules
{
    [TestClass]
    public class UseArrayEmptyAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseArrayEmptyAnalyzer();

        [TestMethod]
        public void EmptyString_ShouldNotReportDiagnosticForEmptyString()
        {
            var test = "";
            VerifyCSharpDiagnostic(test);
        }

        [DataTestMethod]
        [DataRow("new int[0]")]
        [DataRow("new int[] { }")]
        public void EmptyArray_ShouldReportError(string code)
        {
            var test = $"var a = {code};";

            var expected = new DiagnosticResult
            {
                Id = "MA0005",
                Message = "Use Array.Empty<T>()",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 1, column: 9)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [DataTestMethod]
        [DataRow("new int[1]")]
        [DataRow("new int[] { 0 }")]
        public void NonEmptyArray_ShouldReportError(string code)
        {
            var test = $"var a = {code};";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void DynamicLength_ShouldNotReportError()
        {
            var test = @"int length = 0;
var a = new int[length];";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ParamsMethod_ShouldNotReportError()
        {
            var test = @"
public class TestClass
{
    public void Test(params string[] values)
    {
    }

    public void CallTest()
    {
        Test();
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void EmptyArrayInAttribute_ShouldNotReportError()
        {
            var test = "[Test(new int[0])]class TestAttribute : Attribute { }";

            VerifyCSharpDiagnostic(test);
        }
    }
}

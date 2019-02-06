using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.UsageRules
{
    [TestClass]
    public class UseStructLayoutAttributeAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseStructLayoutAttributeAnalyzer();

        [TestMethod]
        public void ShouldNotReportDiagnosticForEmptyString()
        {
            var test = "";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void MissingAttribute_ShouldReportDiagnostic()
        {
            var test = "struct TypeName { }";
            var expected = new DiagnosticResult
            {
                Id = "MA0008",
                Message = "Add StructLayoutAttribute",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 1, column: 0),
                },
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void WithAttribute_ShouldNotReportDiagnostic()
        {
            var test = @"using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Sequential)]
struct TypeName
{
}";
            
            VerifyCSharpDiagnostic(test);
        }
    }
}

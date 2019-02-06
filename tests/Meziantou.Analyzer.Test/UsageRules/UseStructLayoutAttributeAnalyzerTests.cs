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
        protected override string ExpectedDiagnosticId => "MA0008";
        protected override string ExpectedDiagnosticMessage => "Add StructLayoutAttribute";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void ShouldNotReportDiagnosticForEmptyString()
        {
            var test = new ProjectBuilder();
            VerifyDiagnostic(test);
        }

        [TestMethod]
        public void MissingAttribute_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                .WithSource("struct TypeName { }");

            var expected = CreateDiagnosticResult(line: 1, column: 0);

            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void WithAttribute_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                .WithSource(@"using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Sequential)]
struct TypeName
{
}");

            VerifyDiagnostic(project);
        }
    }
}

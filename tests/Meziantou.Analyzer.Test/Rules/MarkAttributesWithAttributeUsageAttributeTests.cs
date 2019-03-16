using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class MarkAttributesWithAttributeUsageAttributeTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new MarkAttributesWithAttributeUsageAttribute();
        protected override string ExpectedDiagnosticId => "MA0010";
        protected override string ExpectedDiagnosticMessage => "Mark attributes with AttributeUsageAttribute";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void ClassInheritsFromAttribute_MissingAttribute_ShouldReportError()
        {
            var project = new ProjectBuilder()
                  .WithSource("class TestAttribute : System.Attribute { }");

            var expected = CreateDiagnosticResult(line: 1, column: 7);
            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void ClassDoesNotInheritsFromAttribute_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .WithSource("class TestAttribute : System.Object { }");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void ClassHasAttribute_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = false, Inherited = true)]
class TestAttribute : System.Attribute { }");

            VerifyDiagnostic(project);
        }
    }
}

using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class UseEventArgsEmptyAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseEventArgsEmptyAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0019";
        protected override string ExpectedDiagnosticMessage => "Use EventArgs.Empty instead of new EventArgs()";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void ShouldReport()
        {
            var project = new ProjectBuilder()
                .WithSourceCode(@"
class TypeName
{
    public void Test()
    {
        new System.EventArgs();
    }
}");
            var expected = CreateDiagnosticResult(line: 6, column: 9);
            VerifyDiagnostic(project, expected);
        }
    }
}

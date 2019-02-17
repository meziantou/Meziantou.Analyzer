using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DoNotUseStringGetHashCodeAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new DoNotUseStringGetHashCodeAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new DoNotUseStringGetHashCodeFixer();
        protected override string ExpectedDiagnosticId => "MA0021";
        protected override string ExpectedDiagnosticMessage => "Use StringComparer.GetHashCode";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void GetHashCode_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TypeName
{
    public void Test()
    {
        ""a"".GetHashCode();
        System.StringComparer.Ordinal.GetHashCode(""a"");
        new object().GetHashCode();
    }
}");
            var expected = CreateDiagnosticResult(line: 6, column: 9);
            VerifyDiagnostic(project, expected);

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        System.StringComparer.Ordinal.GetHashCode(""a"");
        System.StringComparer.Ordinal.GetHashCode(""a"");
        new object().GetHashCode();
    }
}";
            VerifyFix(project, fixtest);
        }
    }
}

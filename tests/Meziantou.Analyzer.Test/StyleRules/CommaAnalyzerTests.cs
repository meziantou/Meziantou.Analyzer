using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.StyleRules
{
    [TestClass]
    public class CommaAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new CommaAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new CommaFixer();
        protected override string ExpectedDiagnosticId => "MA0007";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Info;

        [TestMethod]
        public void EmptyString_ShouldNotReportDiagnosticForEmptyString()
        {
            var project = new ProjectBuilder();
            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void OneLineDeclarationWithMissingTrailingComma_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public async System.Threading.Tasks.Task Test()
    {
        new TypeName() { A = 1 };
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MultipleLinesDeclarationWithTrailingComma_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public async System.Threading.Tasks.Task Test()
    {
        new TypeName()
        {
            A = 1,
            B = 2,
        };
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MultipleLinesDeclarationWithMissingTrailingComma_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public async System.Threading.Tasks.Task Test()
    {
        new TypeName()
        {
            A = 1,
            B = 2
        };
    }
}");

            var expected = CreateDiagnosticResult(line: 12, column: 13, message: "Add comma after the last property");
            VerifyDiagnostic(project, expected);

            var fix = @"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public async System.Threading.Tasks.Task Test()
    {
        new TypeName()
        {
            A = 1,
            B = 2,
        };
    }
}";
            VerifyFix(project, fix);
        }
    }
}

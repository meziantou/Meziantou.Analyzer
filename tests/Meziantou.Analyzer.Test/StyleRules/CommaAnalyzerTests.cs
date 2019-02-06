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

        [TestMethod]
        public void EmptyString_ShouldNotReportDiagnosticForEmptyString()
        {
            var test = "";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void OneLineDeclarationWithMissingTrailingComma_ShouldNotReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public async System.Threading.Tasks.Task Test()
    {
        new TypeName() { A = 1 };
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void MultipleLinesDeclarationWithTrailingComma_ShouldNotReportDiagnostic()
        {
            var test = @"
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

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void MultipleLinesDeclarationWithMissingTrailingComma_ShouldReportDiagnostic()
        {
            var test = @"
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
}";

            var expected = new DiagnosticResult
            {
                Id = "MA0007",
                Message = "Add comma after the last property",
                Severity = DiagnosticSeverity.Info,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 12, column: 13),
                },
            };

            VerifyCSharpDiagnostic(test, expected);

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
            VerifyCSharpFix(test, fix);
        }
    }
}

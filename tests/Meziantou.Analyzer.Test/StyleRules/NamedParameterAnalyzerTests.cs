using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.StyleRules
{
    [TestClass]
    public class NamedParameterAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new NamedParameterAnalyzer();

        [TestMethod]
        public void EmptyString_ShouldNotReportDiagnosticForEmptyString()
        {
            var test = @"";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void FalseConfigureAwait_ShouldNotReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public async System.Threading.Tasks.Task Test()
    {
        await System.Threading.Tasks.Task.Run(()=>{}).ConfigureAwait(false);
    }
}";

            VerifyCSharpDiagnostic(test);
        }
        

        [TestMethod]
        public void NamedParameter_ShouldNotReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        object.Equals(objA: true, "");
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void True_ShouldReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        object.Equals(true, "");
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "MA0003",
                Message = "Name the parameter to improve the readability of the code",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                 {
                    new DiagnosticResultLocation("Test0.cs", line: 6, column: 23)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void False_ShouldReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        object.Equals(false, "");
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "MA0003",
                Message = "Name the parameter to improve the readability of the code",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                 {
                    new DiagnosticResultLocation("Test0.cs", line: 6, column: 23)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void Null_ShouldReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        object.Equals(null, "");
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "MA0003",
                Message = "Name the parameter to improve the readability of the code",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                 {
                    new DiagnosticResultLocation("Test0.cs", line: 6, column: 23)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }
    }
}

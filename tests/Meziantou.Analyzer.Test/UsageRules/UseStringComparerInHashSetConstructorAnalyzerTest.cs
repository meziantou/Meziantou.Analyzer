using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test
{
    [TestClass]
    public class UseStringComparerInHashSetConstructorAnalyzerTest : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseStringComparerInHashSetConstructorAnalyzer();

        [TestMethod]
        public void EmptyString_ShouldNotReportDiagnosticForEmptyString()
        {
            var test = "";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void HashSet_Int32_ShouldNotReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<int>();
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void HashSet_String_ShouldReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<string>();
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "MA0002",
                Message = "Use an overload of the constructor that has a IEqualityComparer<string> parameter",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 6, column: 9)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void HashSet_String_StringEqualityComparer_ShouldNotReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
    }
}";

            VerifyCSharpDiagnostic(test);
        }
    }
}

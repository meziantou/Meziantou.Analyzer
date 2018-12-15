using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test
{
    [TestClass]
    public class UseStringComparisonAnalyzerTest : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new StringComparisonAnalyzer();

        [TestMethod]
        public void Equals_ShouldNotReportDiagnosticForEmptyString()
        {
            var test = @"";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Equals_String_string_StringComparison_ShouldNotReportDiagnosticWhenStringComparisonIsSpecified()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        var a = ""test"";
        string.Equals(a, ""v"", System.StringComparison.Ordinal);
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Equals_String_string_ShouldReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        System.String.Equals(""a"", ""v"");
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "MA0001",
                Message = "Use an overload of 'Equals' that has a StringComparison parameter",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 6, column: 9)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void Equals_String_ShouldReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        ""a"".Equals(""v"");
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "MA0001",
                Message = "Use an overload of 'Equals' that has a StringComparison parameter",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 6, column: 9)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void IndexOf_Char_ShouldNotReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        ""a"".IndexOf('v');
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IndexOf_String_StringComparison_ShouldNotReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        ""a"".IndexOf(""v"", System.StringComparison.Ordinal);
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IndexOf_String_ShouldReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        ""a"".IndexOf(""v"");
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "MA0001",
                Message = "Use an overload of 'IndexOf' that has a StringComparison parameter",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 6, column: 9)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void StartsWith_String_StringComparison_ShouldNotReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        ""a"".StartsWith(""v"", System.StringComparison.Ordinal);
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void StartsWith_String_ShouldReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        ""a"".StartsWith(""v"");
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "MA0001",
                Message = "Use an overload of 'StartsWith' that has a StringComparison parameter",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 6, column: 9)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }
    }
}

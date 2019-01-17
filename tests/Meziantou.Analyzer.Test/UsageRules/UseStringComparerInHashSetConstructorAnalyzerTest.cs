using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test
{
    [TestClass]
    public class UseStringComparerInHashSetConstructorAnalyzerTest : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseStringComparerInHashSetConstructorAnalyzer();

        protected override CodeFixProvider GetCSharpCodeFixProvider() => new UseStringComparerInHashSetConstructorFixer();

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

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
    }
}";
            VerifyCSharpFix(test, fixtest);
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

        [TestMethod]
        public void Dictionary_String_ShouldReportDiagnostic()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.Dictionary<string, int>();
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

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
    }
}";
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void ConcurrentDictionary_String_ShouldReportDiagnostic()
        {
            const string ConcurrentDictionary = "namespace System.Collections.Concurrent { public class ConcurrentDictionary<TKey, TValue> {  public ConcurrentDictionary() { } public ConcurrentDictionary(System.Collections.Generic.IEqualityComparer<TKey> comparer) { } } }";

            var test = ConcurrentDictionary + @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
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

            var fixtest = ConcurrentDictionary + @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Concurrent.ConcurrentDictionary<string, int>(System.StringComparer.Ordinal);
    }
}";
            VerifyCSharpFix(test, fixtest);
        }
    }
}

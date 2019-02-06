using Meziantou.Analyzer.UsageRules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.UsageRules
{
    [TestClass]
    public class UseRegexTimeoutAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseRegexTimeoutAnalyzer();

        public UseRegexTimeoutAnalyzerTests()
        {
            // https://github.com/dotnet/corefx/blob/master/src/System.Text.RegularExpressions/ref/System.Text.RegularExpressions.cs
            AdditionalFiles.Add(@"
namespace System.Text.RegularExpressions
{
    public partial class Capture
    {
    }
    public partial class CaptureCollection
    {
    }
    public partial class Group : System.Text.RegularExpressions.Capture
    {
    }
    public partial class GroupCollection
    {
    }
    public partial class Match : System.Text.RegularExpressions.Group
    {
    }
    public partial class MatchCollection
    {
    }
    public delegate string MatchEvaluator(System.Text.RegularExpressions.Match match);
    public partial class Regex
    {
        public Regex(string pattern) { }
        public Regex(string pattern, System.Text.RegularExpressions.RegexOptions options) { }
        public Regex(string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { }
        public bool IsMatch(string input) { throw null; }
        public bool IsMatch(string input, int startat) { throw null; }
        public static bool IsMatch(string input, string pattern) { throw null; }
        public static bool IsMatch(string input, string pattern, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static bool IsMatch(string input, string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public System.Text.RegularExpressions.Match Match(string input) { throw null; }
        public System.Text.RegularExpressions.Match Match(string input, int startat) { throw null; }
        public System.Text.RegularExpressions.Match Match(string input, int beginning, int length) { throw null; }
        public static System.Text.RegularExpressions.Match Match(string input, string pattern) { throw null; }
        public static System.Text.RegularExpressions.Match Match(string input, string pattern, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static System.Text.RegularExpressions.Match Match(string input, string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public System.Text.RegularExpressions.MatchCollection Matches(string input) { throw null; }
        public System.Text.RegularExpressions.MatchCollection Matches(string input, int startat) { throw null; }
        public static System.Text.RegularExpressions.MatchCollection Matches(string input, string pattern) { throw null; }
        public static System.Text.RegularExpressions.MatchCollection Matches(string input, string pattern, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static System.Text.RegularExpressions.MatchCollection Matches(string input, string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public string Replace(string input, string replacement) { throw null; }
        public string Replace(string input, string replacement, int count) { throw null; }
        public string Replace(string input, string replacement, int count, int startat) { throw null; }
        public static string Replace(string input, string pattern, string replacement) { throw null; }
        public static string Replace(string input, string pattern, string replacement, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static string Replace(string input, string pattern, string replacement, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public static string Replace(string input, string pattern, System.Text.RegularExpressions.MatchEvaluator evaluator) { throw null; }
        public static string Replace(string input, string pattern, System.Text.RegularExpressions.MatchEvaluator evaluator, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static string Replace(string input, string pattern, System.Text.RegularExpressions.MatchEvaluator evaluator, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public string Replace(string input, System.Text.RegularExpressions.MatchEvaluator evaluator) { throw null; }
        public string Replace(string input, System.Text.RegularExpressions.MatchEvaluator evaluator, int count) { throw null; }
        public string Replace(string input, System.Text.RegularExpressions.MatchEvaluator evaluator, int count, int startat) { throw null; }
        public string[] Split(string input) { throw null; }
        public string[] Split(string input, int count) { throw null; }
        public string[] Split(string input, int count, int startat) { throw null; }
        public static string[] Split(string input, string pattern) { throw null; }
        public static string[] Split(string input, string pattern, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static string[] Split(string input, string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public override string ToString() { throw null; }
        public static string Unescape(string str) { throw null; }
    }
    public enum RegexOptions
    {
        Compiled = 8,
        CultureInvariant = 512,
        ECMAScript = 256,
        ExplicitCapture = 4,
        IgnoreCase = 1,
        IgnorePatternWhitespace = 32,
        Multiline = 2,
        None = 0,
        RightToLeft = 64,
        Singleline = 16,
    }
}
");
        }

        [TestMethod]
        public void EmptyString_ShouldNotReportDiagnosticForEmptyString()
        {
            var test = "";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IsMatch_MissingTimeout_ShouldReportError()
        {
            var test = @"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        Regex.IsMatch(""test"", ""[a-z]+"");
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "MA0009",
                Message = "Add timeout parameter",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 6, column: 9),
                },
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void IsMatch_WithTimeout_ShouldNotReportError()
        {
            var test = @"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        Regex.IsMatch(""test"", ""[a-z]+"", RegexOptions.None, System.TimeSpan.FromSeconds(1));
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Ctor_MissingTimeout_ShouldReportError()
        {
            var test = @"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        new Regex(""[a-z]+"");
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "MA0009",
                Message = "Add timeout parameter",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line: 6, column: 9),
                },
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void Ctor_WithTimeout_ShouldNotReportError()
        {
            var test = @"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        new Regex(""[a-z]+"", RegexOptions.None, System.TimeSpan.FromSeconds(1));
    }
}";

            VerifyCSharpDiagnostic(test);
        }
    }
}

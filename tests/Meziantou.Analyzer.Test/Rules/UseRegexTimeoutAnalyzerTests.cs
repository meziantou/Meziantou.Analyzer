using System.Text.RegularExpressions;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class UseRegexTimeoutAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseRegexTimeoutAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0009";
        protected override string ExpectedDiagnosticMessage => "Regular expressions should not be vulnerable to Denial of Service attacks";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void IsMatch_MissingTimeout_ShouldReportError()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Regex))
                  .WithSourceCode(@"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        Regex.IsMatch(""test"", ""[a-z]+"");
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 9);
            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void IsMatch_WithTimeout_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Regex))
                  .WithSourceCode(@"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        Regex.IsMatch(""test"", ""[a-z]+"", RegexOptions.ExplicitCapture, System.TimeSpan.FromSeconds(1));
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Ctor_MissingTimeout_ShouldReportError()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Regex))
                  .WithSourceCode(@"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        new Regex(""[a-z]+"");
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 9);
            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void Ctor_WithTimeout_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Regex))
                  .WithSourceCode(@"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        new Regex(""[a-z]+"", RegexOptions.ExplicitCapture, System.TimeSpan.FromSeconds(1));
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void NonRegexCtor_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Regex))
                  .WithSourceCode(@"
class TestClass
{
    void Test()
    {
        new System.Exception("""");
    }
}");

            VerifyDiagnostic(project);
        }
    }
}

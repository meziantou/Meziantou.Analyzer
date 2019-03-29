using System.Text.RegularExpressions;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class UseRegexOptionsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseRegexTimeoutAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0023";
        protected override string ExpectedDiagnosticMessage => "Add RegexOptions.ExplicitCapture";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [DataTestMethod]
        [DataRow("([a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", false)]
        [DataRow("([a-z]+)", "RegexOptions.None", false)]
        [DataRow("([a-z]+)", "RegexOptions.ExplicitCapture", true)]
        [DataRow("(?<test>[a-z]+)", "RegexOptions.None", true)]
        public void IsMatch_RegexOptions(string regex, string options, bool isValid)
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Regex))
                  .WithSourceCode(@"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        Regex.IsMatch(""test"", """ + regex + @""", " + options + @", default);
    }
}");

            if (isValid)
            {
                VerifyDiagnostic(project);
            }
            else
            {
                VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 35 + regex.Length));
            }
        }

        [DataTestMethod]
        [DataRow("([a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", false)]
        [DataRow("(?<test>[a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", true)]
        [DataRow("[a-z]+", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", true)]
        [DataRow("[a-z]+", "RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase", true)]
        [DataRow("[a-z]+", "RegexOptions.ECMAScript", true)]
        [DataRow("([a-z]+)", "RegexOptions.ECMAScript", true)]
        public void Ctor_RegexOptions(string regex, string options, bool isValid)
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Regex))
                  .WithSourceCode(@"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        new Regex(""" + regex + @""", " + options + @", default);
    }
}");

            if (isValid)
            {
                VerifyDiagnostic(project);
            }
            else
            {
                VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 23 + regex.Length));
            }
        }
    }
}

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

        [TestMethod]
        public void IsMatch_RegexOptions()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Regex))
                  .WithSource(@"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        Regex.IsMatch(""test"", ""[a-z]+"", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, default);
        Regex.IsMatch(""test"", ""[a-z]+"", RegexOptions.ExplicitCapture, default);
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 41);
            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void Ctor_RegexOptions()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Regex))
                  .WithSource(@"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        new Regex(""[a-z]+"", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, default);
        new Regex(""[a-z]+"", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase, default);
        new Regex(""[a-z]+"", RegexOptions.ECMAScript, default);
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 29);
            VerifyDiagnostic(project, expected);
        }
    }
}

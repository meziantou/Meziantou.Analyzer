using System.Text.RegularExpressions;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class UseRegexOptionsAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseRegexTimeoutAnalyzer>();
        }

        [DataTestMethod]
        [DataRow("([a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", false)]
        [DataRow("([a-z]+)", "RegexOptions.None", false)]
        [DataRow("([a-z]+)", "RegexOptions.ExplicitCapture", true)]
        [DataRow("(?<test>[a-z]+)", "RegexOptions.None", true)]
        public async System.Threading.Tasks.Task IsMatch_RegexOptionsAsync(string regex, string options, bool isValid)
        {
            var project = CreateProjectBuilder()
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
                project.ShouldNotReportDiagnostic();
            }
            else
            {
                project.ShouldReportDiagnostic(line: 6, column: 35 + regex.Length);
            }

            await project.ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("([a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", false)]
        [DataRow("(?<test>[a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", true)]
        [DataRow("[a-z]+", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", true)]
        [DataRow("[a-z]+", "RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase", true)]
        [DataRow("[a-z]+", "RegexOptions.ECMAScript", true)]
        [DataRow("([a-z]+)", "RegexOptions.ECMAScript", true)]
        public async System.Threading.Tasks.Task Ctor_RegexOptionsAsync(string regex, string options, bool isValid)
        {
            var project = CreateProjectBuilder()
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
                project.ShouldNotReportDiagnostic();
            }
            else
            {
                project.ShouldReportDiagnostic(line: 6, column: 23 + regex.Length);
            }

            await project.ValidateAsync();
        }
    }
}

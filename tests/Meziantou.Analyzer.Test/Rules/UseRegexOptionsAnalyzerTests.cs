using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class UseRegexOptionsAnalyzerTests
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
        Regex.IsMatch(""test"", """ + regex + @""", " + (isValid ? "" : "[||]") + options + @", default);
    }
}");
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
        new Regex(""" + regex + @""", " + (isValid ? "" : "[||]") + options + @", default);
    }
}");

            await project.ValidateAsync();
        }
    }
}

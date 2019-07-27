using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class UseRegexOptionsAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseRegexTimeoutAnalyzer>();
        }

        [Theory]
        [InlineData("([a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", false)]
        [InlineData("([a-z]+)", "RegexOptions.None", false)]
        [InlineData("([a-z]+)", "RegexOptions.ExplicitCapture", true)]
        [InlineData("(?<test>[a-z]+)", "RegexOptions.None", true)]
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

        [Theory]
        [InlineData("([a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", false)]
        [InlineData("(?<test>[a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", true)]
        [InlineData("[a-z]+", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", true)]
        [InlineData("[a-z]+", "RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase", true)]
        [InlineData("[a-z]+", "RegexOptions.ECMAScript", true)]
        [InlineData("([a-z]+)", "RegexOptions.ECMAScript", true)]
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

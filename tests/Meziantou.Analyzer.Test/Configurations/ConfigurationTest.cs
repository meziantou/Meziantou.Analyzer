using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class ConfigurationTest
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<AbstractTypesShouldNotHaveConstructorsAnalyzer>();
        }

        [TestMethod]
        public async Task SuppressRule()
        {
            var sourceCode = @"
abstract class Test
{
    public Test() { }
}";
            await CreateProjectBuilder()
                    .WithSourceCode(sourceCode)
                    .WithEditorConfig("MA0017.severity = suppress")
                    .ValidateAsync();
        }
    }
}

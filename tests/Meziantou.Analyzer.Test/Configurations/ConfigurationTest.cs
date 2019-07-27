using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class ConfigurationTest
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<AbstractTypesShouldNotHaveConstructorsAnalyzer>();
        }

        [Fact]
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

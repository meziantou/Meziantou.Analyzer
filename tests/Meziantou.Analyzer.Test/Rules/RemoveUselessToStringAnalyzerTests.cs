using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class RemoveUselessToStringAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<RemoveUselessToStringAnalyzer>();
        }

        [Fact]
        public async Task IntToString_ShouldNotReportDiagnostic()
        {
            var project = CreateProjectBuilder()
                  .WithSourceCode(@"
class Test
{
    public void A() => 1.ToString();
}");

            await project.ValidateAsync();
        }

        [Fact]
        public async Task StringToString_ShouldReportDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
class Test
{
    public void A() => [||]"""".ToString();
}")
                  .ValidateAsync();
        }
    }
}

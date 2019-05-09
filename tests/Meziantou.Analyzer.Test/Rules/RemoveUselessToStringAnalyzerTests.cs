using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class RemoveUselessToStringAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<RemoveUselessToStringAnalyzer>();
        }

        [TestMethod]
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

        [TestMethod]
        public async Task StringToString_ShouldReportDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
class Test
{
    public void A() => """".ToString();
}")
                  .ShouldReportDiagnostic(line: 4, column: 24)
                  .ValidateAsync();
        }
    }
}

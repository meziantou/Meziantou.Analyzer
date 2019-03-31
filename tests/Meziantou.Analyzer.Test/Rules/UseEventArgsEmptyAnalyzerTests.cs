using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class UseEventArgsEmptyAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseEventArgsEmptyAnalyzer>();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task ShouldReportAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        new System.EventArgs();
    }
}";
            await CreateProjectBuilder()
                .WithSourceCode(SourceCode)
                .ShouldReportDiagnostic(line: 6, column: 9)
                .ValidateAsync();
        }
    }
}

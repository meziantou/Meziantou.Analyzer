using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class DontUseInstanceFieldsOfTypeAsyncLocalAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DontUseInstanceFieldsOfTypeAsyncLocalAnalyzer>();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task DontReportAsync()
        {
            const string SourceCode = @"
class Test2
{
    int _a;
    static System.Threading.AsyncLocal<int> _b;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task ReportAsync()
        {
            const string SourceCode = @"
class Test2
{
    System.Threading.AsyncLocal<int> _a;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 4, column: 38)
                  .ValidateAsync();
        }
    }
}

using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DontUseInstanceFieldsOfTypeAsyncLocalAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DontUseInstanceFieldsOfTypeAsyncLocalAnalyzer>();
        }

        [Fact]
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

        [Fact]
        public async System.Threading.Tasks.Task ReportAsync()
        {
            const string SourceCode = @"
class Test2
{
    System.Threading.AsyncLocal<int> [||]_a;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}

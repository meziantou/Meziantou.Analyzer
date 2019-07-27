using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DontTagInstanceFieldsWithThreadStaticAttributeAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DontTagInstanceFieldsWithThreadStaticAttributeAnalyzer>();
        }

        [Fact]
        public async System.Threading.Tasks.Task DontReportAsync()
        {
            const string SourceCode = @"
class Test2
{
    int _a;
    [System.ThreadStatic]
    static int _b;
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
    [System.ThreadStatic]
    int [||]_a;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}

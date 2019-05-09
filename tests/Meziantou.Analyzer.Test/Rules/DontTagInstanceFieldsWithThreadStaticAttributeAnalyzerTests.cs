using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class DontTagInstanceFieldsWithThreadStaticAttributeAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DontTagInstanceFieldsWithThreadStaticAttributeAnalyzer>();
        }

        [TestMethod]
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

        [TestMethod]
        public async System.Threading.Tasks.Task ReportAsync()
        {
            const string SourceCode = @"
class Test2
{
    [System.ThreadStatic]
    int _a;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 5, column: 9)
                  .ValidateAsync();
        }
    }
}

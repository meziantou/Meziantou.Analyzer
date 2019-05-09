using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class DontUseDangerousThreadingMethodsAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DontUseDangerousThreadingMethodsAnalyzer>();
        }

        [DataTestMethod]
        [DataRow("Thread.CurrentThread.Abort()")]
        [DataRow("Thread.CurrentThread.Suspend()")]
        [DataRow("Thread.CurrentThread.Resume()")]
        public async System.Threading.Tasks.Task ReportDiagnosticAsync(string text)
        {
            await CreateProjectBuilder()

                  .WithSourceCode(@"using System.Threading;
public class Test
{
    public void A()
    {
        " + text + @";
    }
}")
                  .ShouldReportDiagnostic(line: 6, column: 9)
                  .ValidateAsync();
        }
    }
}

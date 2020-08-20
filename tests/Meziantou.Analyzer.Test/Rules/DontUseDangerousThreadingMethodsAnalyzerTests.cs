using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DontUseDangerousThreadingMethodsAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DontUseDangerousThreadingMethodsAnalyzer>();
        }

        [Theory]
        [InlineData("Thread.CurrentThread.Abort()")]
        [InlineData("Thread.CurrentThread.Suspend()")]
        [InlineData("Thread.CurrentThread.Resume()")]
        public async Task ReportDiagnostic(string text)
        {
            await CreateProjectBuilder()

                  .WithSourceCode(@"using System.Threading;
public class Test
{
    public void A()
    {
        [||]" + text + @";
    }
}")
                  .ValidateAsync();
        }
    }
}

using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DoNotUseFinalizerAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseFinalizerAnalyzer>();
        }

        [Fact]
        public async Task TestFinalizerReportError()
        {
            const string SourceCode = @"
class Test
{
    public Test() { }
    internal void A() { }

    ~[||]Test()
    {
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}

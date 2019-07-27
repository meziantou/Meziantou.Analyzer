using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DoNotUseDestructorAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseDestructorAnalyzer>();
        }

        [Fact]
        public async Task TestDestructorReportError()
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

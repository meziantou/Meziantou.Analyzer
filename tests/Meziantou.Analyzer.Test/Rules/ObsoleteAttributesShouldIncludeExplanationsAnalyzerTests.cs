using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class ObsoleteAttributesShouldIncludeExplanationsAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<ObsoleteAttributesShouldIncludeExplanationsAnalyzer>();
        }

        [Fact]
        public async Task HasMessage()
        {
            const string SourceCode = @"
class Test
{
    [System.Obsolete(""message"")]
    public void A() { }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task HasNoMessage()
        {
            const string SourceCode = @"
class Test
{
    [[|System.Obsolete()|]]
    public void A() { }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

    }
}

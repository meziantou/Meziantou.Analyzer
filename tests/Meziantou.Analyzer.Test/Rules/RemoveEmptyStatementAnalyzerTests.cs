using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class RemoveEmptyStatementAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<RemoveEmptyStatementAnalyzer>();
        }

        [Fact]
        public async Task EmptyStatement()
        {
            const string SourceCode = @"
class Test
{
    public void A()
    {
        [||];
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task EmptyStatementInALabel()
        {
            const string SourceCode = @"
class Test
{
    public void A()
    {
test:
        ;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}

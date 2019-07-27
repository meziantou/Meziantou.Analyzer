using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

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
        public async System.Threading.Tasks.Task EmptyStatementAsync()
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
        public async System.Threading.Tasks.Task EmptyStatementInALabelAsync()
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

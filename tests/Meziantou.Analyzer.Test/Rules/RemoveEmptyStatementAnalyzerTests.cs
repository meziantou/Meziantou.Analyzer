using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class RemoveEmptyStatementAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<RemoveEmptyStatementAnalyzer>();
        }

        [TestMethod]
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

        [TestMethod]
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

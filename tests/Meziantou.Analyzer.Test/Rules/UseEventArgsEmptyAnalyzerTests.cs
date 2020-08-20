using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class UseEventArgsEmptyAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseEventArgsEmptyAnalyzer>();
        }

        [Fact]
        public async Task ShouldReport()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]new System.EventArgs();
    }
}";
            await CreateProjectBuilder()
                .WithSourceCode(SourceCode)
                .ValidateAsync();
        }
    }
}

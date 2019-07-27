using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class ExceptionNameShouldEndWithExceptionAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<ExceptionNameShouldEndWithExceptionAnalyzer>();
        }

        [Fact]
        public async Task NameEndsWithException()
        {
            const string SourceCode = @"
class CustomException : System.Exception
{
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task NameDoesNotEndWithAttribute()
        {
            const string SourceCode = @"
class [||]CustomEx : System.Exception
{
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

    }
}

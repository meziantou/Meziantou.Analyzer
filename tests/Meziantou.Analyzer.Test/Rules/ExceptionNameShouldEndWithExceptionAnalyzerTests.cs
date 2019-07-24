using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class ExceptionNameShouldEndWithExceptionAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<ExceptionNameShouldEndWithExceptionAnalyzer>();
        }

        [TestMethod]
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

        [TestMethod]
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

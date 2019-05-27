using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class EventArgsNameShouldEndWithEventArgsAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<EventArgsNameShouldEndWithEventArgsAnalyzer>();
        }

        [TestMethod]
        public async Task NameEndsWithEventArgs()
        {
            const string SourceCode = @"
class CustomEventArgs : System.EventArgs
{
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task NameDoesNotEndWithEventArgs()
        {
            const string SourceCode = @"
class [|]CustomArgs : System.EventArgs
{
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

    }
}

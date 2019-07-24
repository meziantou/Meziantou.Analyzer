using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class UseEventHandlerOfTAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseEventHandlerOfTAnalyzer>();
        }

        [TestMethod]
        public async Task ValidEvent()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
class Test
{
    event System.EventHandler<System.EventArgs> myevent;
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task ValidEvent_CustomEventArgs()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
class SampleEventArgs : System.EventArgs
{
}

class Test
{
    event System.EventHandler<SampleEventArgs> myevent;
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task ValidEvent_CustomDelegate()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
class SampleEventArgs : System.EventArgs
{
}

delegate void CustomEventHandler(object sender, SampleEventArgs e);

class Test
{
    event CustomEventHandler myevent;
}")
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("System.Action<string>")]
        [DataRow("System.EventHandler<string>")]
        public async Task InvalidEvent(string signature)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
class Test
{
    event " + signature + @" [||]myevent;
}")
                  .ValidateAsync();
        }
    }
}

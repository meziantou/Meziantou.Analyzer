using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseEventHandlerOfTAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseEventHandlerOfTAnalyzer>();
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Theory]
    [InlineData("System.Action<string>")]
    [InlineData("System.EventHandler<string>")]
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

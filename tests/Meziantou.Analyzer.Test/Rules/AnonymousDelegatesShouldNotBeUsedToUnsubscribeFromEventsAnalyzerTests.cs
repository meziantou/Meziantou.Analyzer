using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class AnonymousDelegatesShouldNotBeUsedToUnsubscribeFromEventsAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<AnonymousDelegatesShouldNotBeUsedToUnsubscribeFromEventsAnalyzer>();
        }

        [Fact]
        public async Task UnsubscribeWithLambda()
        {
            const string SourceCode = @"
using System;
class Test
{
    event EventHandler MyEvent;
    void A()
    {
        MyEvent += (sender, e) => { };
        [|MyEvent -= (sender, e) => { }|];
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task UnsubscribeWithAction()
        {
            const string SourceCode = @"
using System;
class Test
{
    event EventHandler MyEvent;
    void A()
    {
        EventHandler handler = (sender, e) => { };
        MyEvent += handler;
        MyEvent -= handler;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task UnsubscribeWithLocalFunction()
        {
            const string SourceCode = @"
using System;
class Test
{
    event EventHandler MyEvent;
    void A()
    {
        MyEvent += Handler;
        MyEvent -= Handler;
    
        void Handler(object sender, EventArgs e) { }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task UnsubscribeWithDelegate()
        {
            const string SourceCode = @"
using System;
class Test
{
    event EventHandler MyEvent;
    void A()
    {
        MyEvent += delegate (object sender, EventArgs e) { };
        [|MyEvent -= delegate (object sender, EventArgs e) { }|];
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task UnsubscribeWithMethod()
        {
            const string SourceCode = @"
using System;
class Test
{
    event EventHandler MyEvent;
    void A()
    {
        MyEvent += Handler;
        MyEvent -= Handler;
    }

    void Handler(object sender, EventArgs e) { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}

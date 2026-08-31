using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.AnonymousDelegatesShouldNotBeUsedToUnsubscribeFromEventsAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AnonymousDelegatesShouldNotBeUsedToUnsubscribeFromEventsAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task UnsubscribeWithLambda()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                event EventHandler MyEvent;
                void A()
                {
                    MyEvent += (sender, e) => { };
                    {|MA0085:MyEvent -= (sender, e) => { }|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnsubscribeWithAction()
    {
        var test = CreateTest();
        test.TestCode = """
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
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnsubscribeWithLocalFunction()
    {
        var test = CreateTest();
        test.TestCode = """
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
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnsubscribeWithDelegate()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                event EventHandler MyEvent;
                void A()
                {
                    MyEvent += delegate (object sender, EventArgs e) { };
                    {|MA0085:MyEvent -= delegate (object sender, EventArgs e) { }|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnsubscribeWithMethod()
    {
        var test = CreateTest();
        test.TestCode = """
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
            }
            """;

        return test.RunAsync();
    }
}

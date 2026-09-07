using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.UseEventHandlerOfTAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseEventHandlerOfTAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task ValidEvent()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                event System.EventHandler<System.EventArgs> myevent;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidEvent_CustomEventArgs()
    {
        var test = CreateTest();
        test.TestCode = """
            class SampleEventArgs : System.EventArgs
            {
            }

            class Test
            {
                event System.EventHandler<SampleEventArgs> myevent;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidEvent_NonGenericEventHandler()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                event System.EventHandler myevent;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidEvent_EventHandlerOfTSenderTEventArgs()
    {
        var test = CreateTest();
        test.TestCode = """
            class SampleEventArgs : System.EventArgs
            {
            }

            class Test
            {
                event System.EventHandler<Test, SampleEventArgs> myevent;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidEvent_EventHandlerOfTSenderTEventArgs_EventArgsDoesNotInheritFromEventArgs()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                event System.EventHandler<Test, string> myevent;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidEvent_CustomDelegate()
    {
        var test = CreateTest();
        test.TestCode = """
            class SampleEventArgs : System.EventArgs
            {
            }

            delegate void CustomEventHandler(object sender, SampleEventArgs e);

            class Test
            {
                event CustomEventHandler myevent;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidEvent_GenericTypeParameterConstraint()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test<TEventArgs> where TEventArgs : System.EventArgs
            {
                event System.EventHandler<TEventArgs> myevent;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterfaceImplementation()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
                event System.Action<string> {|MA0046:myevent|};
            }

            class Test : ITest
            {
                public event System.Action<string> myevent;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitInterfaceImplementation()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
                event System.Action<string> {|MA0046:myevent|};
            }

            class Test : ITest
            {
                event System.Action<string> ITest.myevent { add { } remove { } }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("System.Action")]
    [InlineData("System.Action<string>")]
    [InlineData("System.Action<object, System.EventArgs, int>")]
    [InlineData("System.Action<string, System.EventArgs>")]
    [InlineData("System.Func<object, System.EventArgs, int>")]
    [InlineData("System.EventHandler<string>")]
    public Task InvalidEvent(string signature)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class Test
            {
                event {{signature}} {|MA0046:myevent|};
            }
            """;

        return test.RunAsync();
    }
}

using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.EventsShouldHaveProperArgumentsAnalyzer>;
using EventArgsFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.EventsShouldHaveProperArgumentsAnalyzer,
    Meziantou.Analyzer.Rules.UseEventArgsEmptyFixer>;
using SenderFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.EventsShouldHaveProperArgumentsAnalyzer,
    Meziantou.Analyzer.Rules.EventsShouldHaveProperArgumentsFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EventsShouldHaveProperArgumentsAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    private static SenderFixTest CreateSenderFixTest() => new();

    private static EventArgsFixTest CreateEventArgsFixTest() => new();

    [Fact]
    public Task InvalidArguments_InstanceEvent_ConditionalAccess()
    {
        var test = CreateSenderFixTest();
        test.TestCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    MyEvent?.Invoke({|MA0091:null|}, EventArgs.Empty);
                }
            }
            """;
        test.FixedCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    MyEvent?.Invoke(this, EventArgs.Empty);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidArguments_InstanceEvent()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    MyEvent.Invoke(this, EventArgs.Empty);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidSender_Instance()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    MyEvent.Invoke({|MA0091:null|}, EventArgs.Empty);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidSender_Static()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                public static event EventHandler MyEvent;

                void OnEvent()
                {
                    MyEvent.Invoke({|MA0092:this|}, EventArgs.Empty);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidSender_InstanceEvent_StaticMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                static void Raise(Test instance) => instance.MyEvent?.Invoke(instance, EventArgs.Empty);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidSender_InstanceEvent_StaticLambda()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    Action<Test> raise = static instance => instance.MyEvent?.Invoke(instance, EventArgs.Empty);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidSender_InstanceEvent_StaticMethod_NoCodeFix()
    {
        var code = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                static void Raise(Test instance) => instance.MyEvent?.Invoke({|MA0091:null|}, EventArgs.Empty);
            }
            """;

        var test = CreateSenderFixTest();
        test.TestCode = code;
        test.FixedCode = code;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidEventArgs()
    {
        var test = CreateEventArgsFixTest();
        test.TestCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    MyEvent.Invoke(this, {|MA0093:null|});
                }
            }
            """;
        test.FixedCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    MyEvent.Invoke(this, EventArgs.Empty);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidEventArgs_NamedArgument()
    {
        var test = CreateEventArgsFixTest();
        test.TestCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    MyEvent.Invoke(this, e: {|MA0093:null|});
                }
            }
            """;
        test.FixedCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    MyEvent.Invoke(this, e: EventArgs.Empty);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EventIsStoredInVariable()
    {
        var test = CreateEventArgsFixTest();
        test.TestCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    var ev = MyEvent;
                    if (ev != null)
                    {
                        ev.Invoke(this, {|MA0093:null|});
                    }
                }
            }
            """;
        test.FixedCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    var ev = MyEvent;
                    if (ev != null)
                    {
                        ev.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EventIsStoredInVariableInVariable()
    {
        var test = CreateEventArgsFixTest();
        test.TestCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    var a = MyEvent;
                    var ev = a;
                    if (ev != null)
                    {
                        ev.Invoke(this, {|MA0093:null|});
                    }
                }
            }
            """;
        test.FixedCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    var a = MyEvent;
                    var ev = a;
                    if (ev != null)
                    {
                        ev.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EventIsStoredInVariableAndConditionalAccess()
    {
        var test = CreateEventArgsFixTest();
        test.TestCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    var ev = MyEvent;
                    ev?.Invoke(this, {|MA0093:null|});
                }
            }
            """;
        test.FixedCode = """
            using System;
            class Test
            {
                public event EventHandler MyEvent;

                void OnEvent()
                {
                    var ev = MyEvent;
                    ev?.Invoke(this, EventArgs.Empty);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidEventArgs_GenericTypeParameterConstraint()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            delegate void CustomEventHandler<TEventArgs>(object sender, TEventArgs e) where TEventArgs : EventArgs;
            class Test<TEventArgs> where TEventArgs : EventArgs
            {
                public event CustomEventHandler<TEventArgs> MyEvent;

                void OnEvent()
                {
                    MyEvent?.Invoke(this, {|MA0093:null|});
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CyclicLocalInitializers()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                void OnEvent()
                {
                    EventHandler a = {|CS0841:b|};
                    EventHandler b = a;
                    a.Invoke(this, EventArgs.Empty);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SelfReferencingLocalInitializer()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                void OnEvent()
                {
                    EventHandler a = {|CS0165:a|};
                    a.Invoke(this, EventArgs.Empty);
                }
            }
            """;

        return test.RunAsync();
    }
}

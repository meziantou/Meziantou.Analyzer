using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.EventSourceImplementationAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EventSourceImplementationAnalyzerTests
{
    private static Task RunAsync(string testCode, string expectedRuleId, string expectedMessage)
    {
        var test = new AnalyzerTest { TestCode = testCode };
        test.ExpectedDiagnostics.Add(new DiagnosticResult(expectedRuleId, DiagnosticSeverity.Warning).WithLocation(0).WithMessage(expectedMessage));
        return test.RunAsync();
    }

    [Fact]
    public Task ValidEventSource_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Diagnostics.Tracing;

                sealed class Sample : EventSource
                {
                    [Event(1)]
                    public void Start() => WriteEvent(1);

                    [Event(2)]
                    public void Stop(string name, int count) => WriteEvent(2, name, count);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task NotAnEventSource_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                sealed class Sample
                {
                    public void Start() => WriteEvent(2);

                    private void WriteEvent(int eventId) { }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task NonEventMethod_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Diagnostics.Tracing;

                sealed class Sample : EventSource
                {
                    [NonEvent]
                    public void Start(string name, int count) => WriteEvent(1, name);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task NonPublicMethodWithoutEventAttribute_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Diagnostics.Tracing;

                sealed class Sample : EventSource
                {
                    private void Start(string name, int count) => WriteEvent(1, name);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task ArgumentIsNotAParameter_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Diagnostics.Tracing;

                sealed class Sample : EventSource
                {
                    [Event(1)]
                    public void Start(string name) => WriteEvent(1, name.Trim());
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task ArgumentIsAnEnumConvertedToItsUnderlyingType_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Diagnostics.Tracing;

                enum Level { None }

                sealed class Sample : EventSource
                {
                    [Event(1)]
                    public void Start(Level level) => WriteEvent(1, (int)level);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task ArgumentsProvidedAsAnArray_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Diagnostics.Tracing;

                sealed class Sample : EventSource
                {
                    [Event(1)]
                    public void Start(string name, int count)
                    {
                        object[] args = [name, count];
                        WriteEvent(1, args);
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task UtilityEventSource_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Diagnostics.Tracing;

                abstract class UtilitySample : EventSource
                {
                    protected unsafe void WriteEvent(int eventId, int arg1, long arg2)
                    {
                        EventData* data = stackalloc EventData[2];
                        WriteEventCore(eventId, 2, data);
                    }
                }

                sealed class Sample : UtilitySample
                {
                    [Event(1)]
                    public void Start(int arg1, long arg2) => WriteEvent(1, arg1, arg2);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task WriteEventCore_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Diagnostics.Tracing;

                sealed class Sample : EventSource
                {
                    [Event(1)]
                    public unsafe void Start(int arg1, long arg2)
                    {
                        EventData* data = stackalloc EventData[2];
                        WriteEventCore(1, 2, data);
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task WriteEventWithRelatedActivityId_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System;
                using System.Diagnostics.Tracing;

                sealed class Sample : EventSource
                {
                    [Event(1)]
                    public void Start(Guid relatedActivityId, string name) => WriteEventWithRelatedActivityId(1, relatedActivityId, name);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task SelfDescribingEvents_UnsupportedParameterType_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Diagnostics.Tracing;

                sealed class Sample : EventSource
                {
                    public Sample()
                        : base("Sample", EventSourceSettings.EtwSelfDescribingEventFormat)
                    {
                    }

                    [Event(1)]
                    public void Start(object value) => WriteEvent(1, value);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task MismatchedEventId_Diagnostic()
    {
        return RunAsync("""
            using System.Diagnostics.Tracing;

            sealed class Sample : EventSource
            {
                [Event(1)]
                public void Start() => WriteEvent({|#0:2|});
            }
            """, "MA0234", "'WriteEvent' is called with the event id '2' but the method declares the event id '1'");
    }

    [Fact]
    public Task DuplicatedEventId_Diagnostic()
    {
        return RunAsync("""
            using System.Diagnostics.Tracing;

            sealed class Sample : EventSource
            {
                [Event(1)]
                public void Start() => WriteEvent(1);

                [{|#0:Event(1)|}]
                public void Stop() => WriteEvent(1);
            }
            """, "MA0229", "The event id '1' is already used by the event 'Start'");
    }

    [Fact]
    public Task NonPositiveEventId_Diagnostic()
    {
        return RunAsync("""
            using System.Diagnostics.Tracing;

            sealed class Sample : EventSource
            {
                [{|#0:Event(0)|}]
                public void Start() => WriteEvent(0);
            }
            """, "MA0228", "The event id must be greater than zero");
    }

    [Fact]
    public Task DuplicatedEventName_Diagnostic()
    {
        return RunAsync("""
            using System.Diagnostics.Tracing;

            sealed class Sample : EventSource
            {
                [Event(1)]
                public void Start() => WriteEvent(1);

                [Event(2)]
                public void {|#0:Start|}(string name) => WriteEvent(2, name);
            }
            """, "MA0230", "The event name 'Start' is already used by another event");
    }

    [Fact]
    public Task TooFewArguments_Diagnostic()
    {
        return RunAsync("""
            using System.Diagnostics.Tracing;

            sealed class Sample : EventSource
            {
                [Event(1)]
                public void Start(string name, int count) => {|#0:WriteEvent(1, name)|};
            }
            """, "MA0235", "'WriteEvent' writes 1 payload item(s) but the event method declares 2 payload parameter(s)");
    }

    [Fact]
    public Task TooManyArguments_Diagnostic()
    {
        return RunAsync("""
            using System.Diagnostics.Tracing;

            sealed class Sample : EventSource
            {
                [Event(1)]
                public void Start(string name) => {|#0:WriteEvent(1, name, name)|};
            }
            """, "MA0235", "'WriteEvent' writes 2 payload item(s) but the event method declares 1 payload parameter(s)");
    }

    [Fact]
    public Task InvalidArgumentOrder_Diagnostic()
    {
        return RunAsync("""
            using System.Diagnostics.Tracing;

            sealed class Sample : EventSource
            {
                [Event(1)]
                public void Start(string name, string category) => {|#0:WriteEvent(1, category, name)|};
            }
            """, "MA0236", "'WriteEvent' must write the payload parameters of the event method in the order they are declared");
    }

    [Fact]
    public Task UnsupportedParameterType_Diagnostic()
    {
        return RunAsync("""
            using System;
            using System.Diagnostics.Tracing;

            sealed class Sample : EventSource
            {
                [Event(1)]
                public void Start(Uri {|#0:uri|}) => WriteEvent(1, uri);
            }
            """, "MA0238", "The type 'System.Uri' is not supported by EventSource");
    }

    [Fact]
    public Task StaticEventMethod_Diagnostic()
    {
        return RunAsync("""
            using System.Diagnostics.Tracing;

            sealed class Sample : EventSource
            {
                [Event(1)]
                public static void {|#0:Start|}() { }
            }
            """, "MA0231", "An event method must not be static");
    }

    [Fact]
    public Task AbstractEventSourceDeclaringAnEventMethod_Diagnostic()
    {
        return RunAsync("""
            using System.Diagnostics.Tracing;

            abstract class Sample : EventSource
            {
                [{|#0:Event(1)|}]
                public void Start() => WriteEvent(1);
            }
            """, "MA0233", "An abstract EventSource must not declare event methods");
    }

    [Fact]
    public Task ExplicitInterfaceImplementation_Diagnostic()
    {
        return RunAsync("""
            using System.Diagnostics.Tracing;

            interface ISample
            {
                void Start();
            }

            sealed class Sample : EventSource, ISample
            {
                [Event(1)]
                void ISample.{|#0:Start|}() => WriteEvent(1);
            }
            """, "MA0232", "An event method must not be an explicit interface implementation");
    }

    [Fact]
    public Task InvalidEventDataCount_Diagnostic()
    {
        return RunAsync("""
            using System.Diagnostics.Tracing;

            sealed class Sample : EventSource
            {
                [Event(1)]
                public unsafe void Start(int arg1, long arg2)
                {
                    EventData* data = stackalloc EventData[2];
                    WriteEventCore(1, {|#0:1|}, data);
                }
            }
            """, "MA0235", "'WriteEventCore' writes 1 payload item(s) but the event method declares 2 payload parameter(s)");
    }

    [Fact]
    public Task MissingRelatedActivityIdParameter_Diagnostic()
    {
        return RunAsync("""
            using System;
            using System.Diagnostics.Tracing;

            sealed class Sample : EventSource
            {
                [Event(1)]
                public void Start(Guid activityId, string name) => {|#0:WriteEventWithRelatedActivityId(1, activityId, name)|};
            }
            """, "MA0237", "The first parameter of an event method calling 'WriteEventWithRelatedActivityId' must be a 'Guid' named 'relatedActivityId'");
    }
}

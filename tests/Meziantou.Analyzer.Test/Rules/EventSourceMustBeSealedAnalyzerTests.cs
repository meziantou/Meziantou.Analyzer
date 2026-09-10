using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.EventSourceMustBeSealedAnalyzer,
    Meziantou.Analyzer.Rules.EventSourceMustBeSealedFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EventSourceMustBeSealedAnalyzerTests
{
    [Fact]
    public Task SealedEventSource_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System.Diagnostics.Tracing;

                sealed class Sample : EventSource
                {
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task AbstractEventSource_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System.Diagnostics.Tracing;

                abstract class Sample : EventSource
                {
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task NotAnEventSource_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task UnsealedEventSource_Diagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System.Diagnostics.Tracing;

                class [|Sample|] : EventSource
                {
                }
                """,
            FixedCode = """
                using System.Diagnostics.Tracing;

                sealed class Sample : EventSource
                {
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task UnsealedEventSource_InheritsFromAbstractEventSource_Diagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System.Diagnostics.Tracing;

                abstract class BaseSample : EventSource
                {
                }

                class [|Sample|] : BaseSample
                {
                }
                """,
            FixedCode = """
                using System.Diagnostics.Tracing;

                abstract class BaseSample : EventSource
                {
                }

                sealed class Sample : BaseSample
                {
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task SealedEventSource_InheritsFromAbstractEventSource_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                using System.Diagnostics.Tracing;

                abstract class BaseSample : EventSource
                {
                }

                sealed class Sample : BaseSample
                {
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task UnsealedEventSourceFromNuGetPackage_Diagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                using Microsoft.Diagnostics.Tracing;

                class [|Sample|] : EventSource
                {
                }

                namespace Microsoft.Diagnostics.Tracing
                {
                    public class EventSource
                    {
                    }
                }
                """,
            FixedCode = """
                using Microsoft.Diagnostics.Tracing;

                sealed class Sample : EventSource
                {
                }

                namespace Microsoft.Diagnostics.Tracing
                {
                    public class EventSource
                    {
                    }
                }
                """,
        }.RunAsync();
    }
}

#pragma warning disable CA1030 // Use events where appropriate

using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotRaiseApplicationExceptionAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotRaiseApplicationExceptionAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task RaiseNotReservedException_ShouldNotReportErrorAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class TestAttribute
            {
                void Test()
                {
                    throw new Exception();
                    throw new ArgumentException();

                    try
                    {
                    }
                    catch (ApplicationException)
                    {
                        throw;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RaiseReservedException_ShouldReportErrorAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class TestAttribute
            {
                void Test()
                {
                    [|throw new ApplicationException();|]
                }
            }
            """;

        return test.RunAsync();
    }
}

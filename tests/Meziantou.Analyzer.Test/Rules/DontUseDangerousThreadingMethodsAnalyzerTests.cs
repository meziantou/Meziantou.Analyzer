using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DontUseDangerousThreadingMethodsAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DontUseDangerousThreadingMethodsAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Theory]
    [InlineData("Thread.CurrentThread.Abort()")]
    [InlineData("Thread.CurrentThread.Suspend()")]
    [InlineData("Thread.CurrentThread.Resume()")]
    public Task ReportDiagnostic(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Threading;
            public class Test
            {
                public void A()
                {
                    [|{{text}}|];
                }
            }
            """;

        return test.RunAsync();
    }
}

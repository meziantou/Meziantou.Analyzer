using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseAsyncVoidAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseAsyncVoidAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new() { ReferenceAssemblies = ReferenceAssemblies.Net.Net80 };

    [Fact]
    public Task Method_Void()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                void A() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Method_AsyncVoid()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                async void {|MA0155:A|}() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Method_AsyncTask()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                async System.Threading.Tasks.Task A() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalFunction_Void()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                void A()
                {
                  void Local() => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalFunction_AsyncVoid()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                void A()
                {
                  {|MA0155:async void Local() => throw null;|}
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalFunction_AsyncTask()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                void A()
                {
                  async System.Threading.Tasks.Task Local() => throw null;
                }
            }
            """;

        return test.RunAsync();
    }
}

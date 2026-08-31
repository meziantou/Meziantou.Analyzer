using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseAsyncDelegateForSyncDelegateAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseAsyncDelegateForSyncDelegateAnalyzerTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task List_ForEach_Sync()
    {
        var test = CreateTest();
        test.TestCode = """
            new System.Collections.Generic.List<int>().ForEach(item => {});
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task List_ForEach_Async()
    {
        var test = CreateTest();
        test.TestCode = """
            new System.Collections.Generic.List<int>().ForEach({|MA0147:async item => {}|});
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CustomDelegate_Sync()
    {
        var test = CreateTest();
        test.TestCode = """
            A(() => {});

            void A(D a) => throw null;
            delegate void D();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CustomDelegate_AsyncVoid()
    {
        var test = CreateTest();
        test.TestCode = """
            A({|MA0147:async () => {}|});

            void A(D a) => throw null;
            delegate void D();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Action_Sync()
    {
        var test = CreateTest();
        test.TestCode = """
            A(() => {});

            void A(System.Action a) => throw null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Action_AsyncVoid()
    {
        var test = CreateTest();
        test.TestCode = """
            A({|MA0147:async () => {}|});

            void A(System.Action a) => throw null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FuncTask_AsyncDelegate()
    {
        var test = CreateTest();
        test.TestCode = """
            A(async () => {});

            void A(System.Func<System.Threading.Tasks.Task> a) => throw null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FuncValueTask_AsyncDelegate()
    {
        var test = CreateTest();
        test.TestCode = """
            A(async () => {});

            void A(System.Func<System.Threading.Tasks.ValueTask> a) => throw null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FuncValueTaskOfInt_AsyncDelegate()
    {
        var test = CreateTest();
        test.TestCode = """
            A(async () => 1);

            void A(System.Func<System.Threading.Tasks.ValueTask<int>> a) => throw null;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Event_AsyncVoid()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample.A += async (sender, e) => { };
            Sample.A -= async (sender, e) => { };

            class Sample
            {
                public static event System.EventHandler A;
            }
            """;

        return test.RunAsync();
    }
}

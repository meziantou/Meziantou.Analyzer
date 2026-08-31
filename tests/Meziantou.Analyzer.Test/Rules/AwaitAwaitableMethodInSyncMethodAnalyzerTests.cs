using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.AwaitAwaitableMethodInSyncMethodAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AwaitAwaitableMethodInSyncMethodAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task NoReport_NonAwaitedTaskInAsyncMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                async Task A()
                {
                    Task.Delay(0);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoReport_AwaitedTaskInAsyncMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                async Task A()
                {
                    await Task.Delay(0);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoReport_TaskInAsyncLocalFunction()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    B();
                    async void B()
                    {
                        Task.Delay(0);
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoReport_TaskInAsyncLambdaFunction()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    _ = new System.Action(async () =>
                    {
                        Task.Delay(0);
                    });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoReport_Discard()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    _ = Task.Delay(0);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_Discard_WhenConfigured()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0134.report_discarded", "true");
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    _ = {|MA0134:Task.Delay(0)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_Discard_InConstructor_WhenConfigured()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0134.report_discarded", "true");
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Test()
                {
                    _ = {|MA0134:StartAsync()|};
                }

                Task StartAsync() => Task.Delay(0);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoReport_TopLevelStatement()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Threading.Tasks;

            Task.Delay(0);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoReport_FireAndForget()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    Task.Delay(0).FireAndForget();
                }
            }

            static class Extensions
            {
                public static void FireAndForget(this Task task) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_TaskInSyncMethodReturningTask()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task A()
                {
                    {|MA0134:Task.Delay(0)|};
                    return Task.CompletedTask;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_NonAwaitableTypeAttribute_TaskWrappedType_InSyncMethod()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(Result))]

            class Test
            {
                void A()
                {
                    {|MA0134:B()|};
                }

                Task<Result> B() => throw null;
            }

            class Result { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_NonAwaitableTypeAttribute_OpenGenericTaskWrappedType_InSyncMethod()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(Result<>))]

            class Test
            {
                void A()
                {
                    {|MA0134:B()|};
                }

                Task<Result<int>> B() => throw null;
            }

            class Result<T> { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_TaskInSyncVoidMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    {|MA0134:Task.Delay(0)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_CustomAwaitableInSyncMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    {|MA0134:Task.Yield()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_TaskInLambda()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    _ = new System.Action(() =>
                    {
                        {|MA0134:Task.Delay(0)|};
                    });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_TaskInLambda_Arrow()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    _ = new System.Action(() => {|MA0134:Task.Delay(0)|});
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_TaskInDelegate()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    _ = new System.Action(delegate
                    {
                        {|MA0134:Task.Delay(0)|};
                    });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_TaskInDelegate_Parentheses()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    _ = new System.Action(delegate()
                    {
                        {|MA0134:Task.Delay(0)|};
                    });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_TaskInGetter()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                int A
                {
                    get
                    {
                        {|MA0134:Task.Delay(0)|};
                        return 0;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_TaskInLocalFunction()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    B();
                    void B()
                    {
                        {|MA0134:Task.Delay(0)|};
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_TaskInLocalTaskReturningFunction()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                async Task A()
                {
                    await B();

                    Task B()
                    {
                        {|MA0134:Task.Delay(0)|};
                        return Task.CompletedTask;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_TaskConfigureAwait()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    {|MA0134:Task.Delay(0).ConfigureAwait(false)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_ConditionalInvoke()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task ReturnTask() => throw null;

                void A(Test instance)
                {
                    instance?{|MA0134:.ReturnTask()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DoNotReport_Discard_ConditionalInvoke()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task ReturnTask() => throw null;

                void A(Test instance)
                {
                    _ = instance?.ReturnTask();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_Discard_ConditionalInvoke_WhenConfigured()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0134.report_discarded", "true");
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task ReturnTask() => throw null;

                void A(Test instance)
                {
                    _ = instance?{|MA0134:.ReturnTask()|};
                }
            }
            """;

        return test.RunAsync();
    }
}

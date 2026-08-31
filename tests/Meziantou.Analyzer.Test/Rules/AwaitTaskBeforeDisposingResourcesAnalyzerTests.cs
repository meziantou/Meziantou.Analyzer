using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.AwaitTaskBeforeDisposingResourcesAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AwaitTaskBeforeDisposingResourcesAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task NotAwaitedTask_InUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                Task Test()
                {
                    using ((IDisposable)null)
                    {
                        {|MA0100:return Task.Delay(1);|}
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAwaitedTaskMethod_InUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                Task<int> Test()
                {
                    using ((IDisposable)null)
                    {
                        {|MA0100:return TestAsync().AsTask();|}
                    }
                }

                async ValueTask<int> TestAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAwaitedTaskYieldMethod_InUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                object Test()
                {
                    using ((IDisposable)null)
                    {
                        // Custom awaitable type (not Task/ValueTask)
                        {|MA0100:return Task.Yield();|}
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAwaitedExtensionMethodOnInt32_InUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            static class TestClass
            {
                static object Test()
                {
                    using ((IDisposable)null)
                    {
                        // It should detect the extension method
                        {|MA0100:return 1;|}
                    }
                }

                static System.Runtime.CompilerServices.TaskAwaiter GetAwaiter(this int value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAwaitedExtensionMethodOnValueTuple_InUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            static class TestClass
            {
                static object Test()
                {
                    using ((IDisposable)null)
                    {
                        // It should detect the extension method
                        {|MA0100:return (default(Task<int>), default(Task<string>));|}
                    }
                }

                static System.Runtime.CompilerServices.TaskAwaiter<(T1, T2)> GetAwaiter<T1, T2>(this (Task<T1>, Task<T2>) tasks) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAwaitedValueTask_InUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                ValueTask Test()
                {
                    using ((IDisposable)null)
                    {
                        {|MA0100:return new ValueTask(Task.Delay(1));|}
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitedTaskInUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                async Task<int> Test()
                {
                    using ((IDisposable)null)
                    {
                        return await Task.FromResult(1);
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAwaitedTaskFromResultInUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                Task<int> Test()
                {
                    using ((IDisposable)null)
                    {
                        return Task.FromResult(1);
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAwaitedTaskFromResultInUsingVariable()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                Task<int> Test()
                {
                    using var a = (IDisposable)null;
                    return Task.FromResult(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAwaitedTask_NotInUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                Task Test()
                {
                    return Task.Delay(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAwaitedValueTaskWithValue_InUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                ValueTask<int> Test()
                {
                    using ((IDisposable)null)
                    {
                        return new ValueTask<int>(1);
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAwaitedValueTaskWithTaskValue_InUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                ValueTask<int> Test()
                {
                    using ((IDisposable)null)
                    {
                        {|MA0100:return new ValueTask<int>(Task.FromResult(1));|}
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAwaitedNullTask_InUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                Task Test()
                {
                    using ((IDisposable)null)
                    {
                        return null;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAwaitedDefaultTask_InUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                Task Test()
                {
                    using ((IDisposable)null)
                    {
                        return default;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAwaitedDefaultValueTask_InUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                ValueTask Test()
                {
                    using ((IDisposable)null)
                    {
                        return default;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAwaitedNewValueTask_InUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                ValueTask Test()
                {
                    using ((IDisposable)null)
                    {
                        return new ValueTask();
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAwaitedTaskCompleted_InUsing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                Task Test()
                {
                    using ((IDisposable)null)
                    {
                        return Task.CompletedTask;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReturnWithoutValue()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    return;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TaskRun()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class TestClass
            {
                async Task Test()
                {
                    using ((IDisposable)null)
                    {
                        await Task.Run(() => Task.Delay(1));
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("IssueId", "https://github.com/meziantou/Meziantou.Analyzer/issues/219")]
    public Task Lambda()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Net.Http;
            using System.Threading.Tasks;
            class TestClass
            {
                public static async Task AnalyzerExample()
                {
                    using ((IDisposable)null)
                    {
                        await ExecuteAsync(() => new HttpClient().GetAsync(new Uri("https://www.meziantou.net/"))).ConfigureAwait(false);
                    }

                    using ((IDisposable)null)
                    {
                        await ExecuteAsync(async () => await new HttpClient().GetAsync(new Uri("https://www.meziantou.net/"))).ConfigureAwait(false);
                    }

                    async Task ExecuteAsync(Func<Task> operation)
                    {
                        // we await the operation there
                        await operation().ConfigureAwait(false);
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReturnInUsingStatement()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp8;
        test.TestCode = """
            class TestClass
            {
                System.Threading.Tasks.Task Test()
                {
                    using var disposable = (System.IDisposable)null;
                    {|MA0100:return System.Threading.Tasks.Task.Delay(1);|}
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingBlockBeforeAReturn()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp8;
        test.TestCode = """
            class TestClass
            {
                System.Threading.Tasks.Task Test()
                {
                    using (var disposable = (System.IDisposable)null)
                    {
                    }

                    return System.Threading.Tasks.Task.Delay(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingBeforeAReturnWithLabel()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp8;
        test.TestCode = """
            class TestClass
            {
                System.Threading.Tasks.Task Test(bool test)
                {
                    if (test) goto a;
                        return System.Threading.Tasks.Task.Delay(1);

                    a:
                    using var disposable = (System.IDisposable) null;
                    {|MA0100:return System.Threading.Tasks.Task.Delay(1);|}
                }

            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExecutionContext_SuppressFlow_NoAlert()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class TestClass
            {
                protected Task RunInNewContextAsync(Func<Task> func, CancellationToken ct)
                {
                    using (ExecutionContext.SuppressFlow())
                    {
                        // This is safe because ExecutionContext is captured at task creation
                        // No need to await before the using block ends
                        return Task.Run(func, ct);
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExecutionContext_SuppressFlow_UsingDeclaration_NoAlert()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class TestClass
            {
                protected Task RunInNewContextAsync(Func<Task> func, CancellationToken ct)
                {
                    using var flowControl = ExecutionContext.SuppressFlow();
                    // This is safe because ExecutionContext is captured at task creation
                    // No need to await before the using block ends
                    return Task.Run(func, ct);
                }
            }
            """;

        return test.RunAsync();
    }
}

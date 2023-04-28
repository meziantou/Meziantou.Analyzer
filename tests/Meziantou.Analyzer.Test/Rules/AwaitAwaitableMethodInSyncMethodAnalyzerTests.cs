using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class AwaitAwaitableMethodInSyncMethodAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<AwaitAwaitableMethodInSyncMethodAnalyzer>();
    }

    [Fact]
    public async Task NoReport_NonAwaitedTaskInAsyncMethod()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                   using System.Threading.Tasks;
                   class Test
                   {
                       async Task A()
                       {
                           Task.Delay(0);
                       }
                   }
                   """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoReport_AwaitedTaskInAsyncMethod()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                   using System.Threading.Tasks;
                   class Test
                   {
                       async Task A()
                       {
                           await Task.Delay(0);
                       }
                   }
                   """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoReport_TaskInAsyncLocalFunction()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NoReport_TaskInAsyncLambdaFunction()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NoReport_Discard()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    void A()
                    {
                        _ = Task.Delay(0);
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NoReport_DiscardConditionalAccess()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;

                public interface IFoo
                {
                    Task BarAsync();
                }
                
                class Test
                {
                    public static void Baz(IFoo? foo)
                    {
                        _ = foo?.BarAsync();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NoReport_TopLevelStatement()
    {
        await CreateProjectBuilder()
            .WithOutputKind(OutputKind.ConsoleApplication)
            .WithSourceCode("""
                using System.Threading.Tasks;

                Task.Delay(0);
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NoReport_FireAndForget()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Report_TaskInSyncMethodReturningTask()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task A()
                    {
                        [||]Task.Delay(0);
                        return Task.CompletedTask;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Report_TaskInSyncVoidMethod()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    void A()
                    {
                        [||]Task.Delay(0);
                    }
                }
                """)
            .ValidateAsync();
    }


    [Fact]
    public async Task Report_TaskInSyncVoidMethod_ConditionalAccess()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                
                public interface IFoo
                {
                    Task BarAsync();
                }
                
                class Test
                {
                    public static void Baz(IFoo? foo)
                    {
                        [||]foo?.BarAsync();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Report_CustomAwaitableInSyncMethod()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    void A()
                    {
                        [||]Task.Yield();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Report_TaskInLambda()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    void A()
                    {
                        _ = new System.Action(() =>
                        {
                            [||]Task.Delay(0);
                        });
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Report_TaskInLambda_Arrow()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    void A()
                    {
                        _ = new System.Action(() => [||]Task.Delay(0));
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Report_TaskInDelegate()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    void A()
                    {
                        _ = new System.Action(delegate
                        {
                            [||]Task.Delay(0);
                        });
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Report_TaskInDelegate_Parentheses()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    void A()
                    {
                        _ = new System.Action(delegate()
                        {
                            [||]Task.Delay(0);
                        });
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Report_TaskInGetter()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    int A
                    {
                        get
                        {
                            [||]Task.Delay(0);
                            return 0;
                        }
                    }
                }
                """)
            .ValidateAsync();
    }
    
    [Fact]
    public async Task Report_TaskInLocalFunction()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    void A()
                    {
                        B();
                        void B()
                        {
                            [||]Task.Delay(0);
                        }
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Report_TaskConfigureAwait()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    void A()
                    {
                        [||]Task.Delay(0).ConfigureAwait(false);
                    }
                }
                """)
            .ValidateAsync();
    }


    [Fact]
    public async Task Report_ConditionalInvoke()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task ReturnTask() => throw null;

                    void A(Test instance)
                    {
                        instance?[|.ReturnTask()|];
                    }
                }
                """)
            .ValidateAsync();
    }
}

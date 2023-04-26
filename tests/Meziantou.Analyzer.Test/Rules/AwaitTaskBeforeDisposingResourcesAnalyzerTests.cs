using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public class AwaitTaskBeforeDisposingResourcesAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.Net5_0)
            .WithAnalyzer<AwaitTaskBeforeDisposingResourcesAnalyzer>();
    }

    [Fact]
    public async Task NotAwaitedTask_InUsing()
    {
        var originalCode = @"
using System;
using System.Threading.Tasks;
class TestClass
{
    Task Test()
    {
        using ((IDisposable)null)
        {
            [||]return Task.Delay(1);
        }
    }
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task NotAwaitedTaskMethod_InUsing()
    {
        var originalCode = @"
using System;
using System.Threading.Tasks;
class TestClass
{
    Task<int> Test()
    {
        using ((IDisposable)null)
        {
            [||]return TestAsync().AsTask();
        }
    }

    async ValueTask<int> TestAsync() => throw null;
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }
    
    [Fact]
    public async Task NotAwaitedTaskYieldMethod_InUsing()
    {
        var originalCode = @"
using System;
using System.Threading.Tasks;
class TestClass
{
    object Test()
    {
        using ((IDisposable)null)
        {
            // Custom awaitable type (not Task/ValueTask)
            [||]return Task.Yield();
        }
    }
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }
    
    [Fact]
    public async Task NotAwaitedExtensionMethodOnInt32_InUsing()
    {
        var originalCode = @"
using System;
using System.Threading.Tasks;
static class TestClass
{
    static object Test()
    {
        using ((IDisposable)null)
        {
            // It should detect the extension method
            [||]return 1;
        }
    }

    static System.Runtime.CompilerServices.TaskAwaiter GetAwaiter(this int value) => throw null;
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }
    
    [Fact]
    public async Task NotAwaitedExtensionMethodOnValueTuple_InUsing()
    {
        var originalCode = @"
using System;
using System.Threading.Tasks;
static class TestClass
{
    static object Test()
    {
        using ((IDisposable)null)
        {
            // It should detect the extension method
            [||]return (default(Task<int>), default(Task<string>));
        }
    }

    static System.Runtime.CompilerServices.TaskAwaiter<(T1, T2)> GetAwaiter<T1, T2>(this (Task<T1>, Task<T2>) tasks) => throw null;
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task NotAwaitedValueTask_InUsing()
    {
        var originalCode = @"
using System;
using System.Threading.Tasks;
class TestClass
{
    ValueTask Test()
    {
        using ((IDisposable)null)
        {
            [||]return new ValueTask(Task.Delay(1));
        }
    }
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task AwaitedTaskInUsing()
    {
        var originalCode = @"
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
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task NonAwaitedTaskFromResultInUsing()
    {
        var originalCode = @"
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
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task NonAwaitedTaskFromResultInUsingVariable()
    {
        var originalCode = @"
using System;
using System.Threading.Tasks;
class TestClass
{
    Task<int> Test()
    {
        using var a = (IDisposable)null;
        return Task.FromResult(1);
    }
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task NotAwaitedTask_NotInUsing()
    {
        var originalCode = @"
using System;
using System.Threading.Tasks;
class TestClass
{
    Task Test()
    {
        return Task.Delay(1);
    }
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task NotAwaitedValueTaskWithValue_InUsing()
    {
        var originalCode = @"
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
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task NotAwaitedValueTaskWithTaskValue_InUsing()
    {
        var originalCode = @"
using System;
using System.Threading.Tasks;
class TestClass
{
    ValueTask<int> Test()
    {
        using ((IDisposable)null)
        {
            [||]return new ValueTask<int>(Task.FromResult(1));
        }
    }
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task NotAwaitedNullTask_InUsing()
    {
        var originalCode = @"
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
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task NotAwaitedDefaultTask_InUsing()
    {
        var originalCode = @"
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
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }
    
    [Fact]
    public async Task NotAwaitedDefaultValueTask_InUsing()
    {
        var originalCode = @"
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
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task NotAwaitedNewValueTask_InUsing()
    {
        var originalCode = @"
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
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task NotAwaitedTaskCompleted_InUsing()
    {
        var originalCode = @"
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
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task ReturnWithoutValue()
    {
        var originalCode = @"
class TestClass
{
    void Test()
    {
        return;
    }
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task TaskRun()
    {
        var originalCode = @"
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
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    [Trait("IssueId", "https://github.com/meziantou/Meziantou.Analyzer/issues/219")]
    public async Task Lambda()
    {
        var originalCode = @"
using System;
using System.Net.Http;
using System.Threading.Tasks;
class TestClass
{
    public static async Task AnalyzerExample()
    {
        using ((IDisposable)null)
        {
            await ExecuteAsync(() => new HttpClient().GetAsync(new Uri(""https://www.meziantou.net/""))).ConfigureAwait(false);
        }

        using ((IDisposable)null)
        {
            await ExecuteAsync(async () => await new HttpClient().GetAsync(new Uri(""https://www.meziantou.net/""))).ConfigureAwait(false);
        }

        async Task ExecuteAsync(Func<Task> operation)
        {
            // we await the operation there
            await operation().ConfigureAwait(false);
        }
    }
}";

        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }
}

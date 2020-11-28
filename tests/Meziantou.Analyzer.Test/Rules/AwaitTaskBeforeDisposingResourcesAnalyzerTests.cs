using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public class AwaitTaskBeforeDisposingResourcesAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithTargetFramework(TargetFramework.Net50)
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
    }
}

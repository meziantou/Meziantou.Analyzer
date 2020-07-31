using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DoNotUseBlockingCallInAsyncContextAnalyzer_AsyncContextTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithTargetFramework(TargetFramework.NetStandard2_1)
                .WithAnalyzer<DoNotUseBlockingCallInAsyncContextAnalyzer>(id: "MA0042");
        }

        [Fact]
        public async Task Async_Wait_Diagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        [||]Task.Delay(1).Wait();
    }
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task Async_Result_Diagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        _ = [||]Task.FromResult(1).Result;
    }
}")
                  .ValidateAsync();
        }
        
        [Fact]
        public async Task Async_ValueTask_Result_Diagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        _ = [||]new ValueTask<int>(10).Result;
    }
}")
                  .ValidateAsync();
        }
        
        [Fact]
        public async Task Async_ValueTask_GetAwaiter_Diagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        _ = [||]new ValueTask<int>(10).GetAwaiter().GetResult();
    }
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task Async_ThreadSleep_Diagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        [||]System.Threading.Thread.Sleep(1);
    }
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task Async_SuggestOverload_Diagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        [||]Write();
    }

    public void Write() => throw null;
    public Task Write(System.Threading.CancellationToken cancellationToken) => throw null;
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task Async_AsyncSuffix_Diagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        [||]Write();
    }

    public void Write() => throw null;
    public Task WriteAsync() => throw null;
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task Async_NoOverload_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        Write();
    }

    public void Write() => throw null;
    public void WriteAsync() => throw null;
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task AsyncLambda_Overload_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        System.Func<Task> a = async () => [||]Write();
    }

    public void Write() => throw null;
    public Task WriteAsync() => throw null;
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task AsyncLocalFunction_Overload_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public void A()
    {
        Local();

        async Task Local() => [||]Write();
    }

    public void Write() => throw null;
    public Task WriteAsync() => throw null;
}")
                  .ValidateAsync();
        }
        
        [Fact]
        public async Task AsyncLocalFunction_Overload_ValueTask_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public void A()
    {
        Local();

        async Task Local() => [||]Write();
    }

    public void Write() => throw null;
    public ValueTask WriteAsync() => throw null;
}")
                  .ValidateAsync();
        }
        
        [Fact]
        public async Task Method_NoOverload_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        Write();
    }

    public void Write() => throw null;
    public void Write(System.Threading.CancellationToken cancellationToken) => throw null;
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task Method_NoOverloadWithSameParameters_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        Write();
    }

    public void Write() => throw null;
    public Task Write(int a) => throw null;
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task Console_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        System.Console.Out.WriteLine();
        System.Console.Out.Write(' ');
        System.Console.Out.Flush();

        System.Console.Error.WriteLine();
        System.Console.Error.Write(' ');
        System.Console.Error.Flush();
    }
}")
                  .ValidateAsync();
        }
    }
}

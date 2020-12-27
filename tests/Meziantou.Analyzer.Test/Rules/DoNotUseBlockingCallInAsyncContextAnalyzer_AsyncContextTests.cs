using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

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
        [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/169")]
        public async Task AsyncMethodWithAsyncOverload()
        {
            await CreateProjectBuilder()
                    .AddSystemTextJson()
                    .WithSourceCode(@"
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var responseStream = new MemoryStream();
        var SerializerOptions = new JsonSerializerOptions();
        var ct = CancellationToken.None;
        await JsonSerializer.DeserializeAsync<Program>(responseStream, SerializerOptions, ct).ConfigureAwait(false);
    }
}
")
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

        [Fact]
        public async Task ProcessWaitForExit_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
using System.Threading.Tasks;
using System.Diagnostics;

class Test
{
    public async Task A()
    {
        var process = new Process();
        process.WaitForExit();
    }
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task Using_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp9)
                  .WithSourceCode(@"
using System;
using System.Threading.Tasks;
using System.Diagnostics;

class Test
{
    public async Task A()
    {
        using var a = new Sample();
        using (var b = new Sample()) { }
        
    }

    private class Sample : IDisposable
    {
        public void Dispose() => throw null;
    }
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task Using_Diagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
using System;
using System.Threading.Tasks;
using System.Diagnostics;

class Test
{
    public async Task A()
    {
        [||]using var a = new Sample();
        [||]using (var b = new Sample()) { }

        var sample = new Sample();
        [||]using (sample) { }

        await using var c = new Sample();
        await using (var d = new Sample()) { }
    }

    private class Sample : IDisposable
    {
        public void Dispose() => throw null;
        public ValueTask DisposeAsync() => throw null;
    }
}")
                  .ValidateAsync();
        }
    }
}

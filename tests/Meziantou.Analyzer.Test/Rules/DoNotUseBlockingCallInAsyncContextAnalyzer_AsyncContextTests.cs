using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseBlockingCallInAsyncContextAnalyzer_AsyncContextTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.NetStandard2_1)
            .WithAnalyzer<DoNotUseBlockingCallInAsyncContextAnalyzer>(id: "MA0042")
            .WithCodeFixProvider<DoNotUseBlockingCallInAsyncContextFixer>();
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
              .ShouldFixCodeWith(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        await Task.Delay(1);
    }
}")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task FixerShouldAddParentheses()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        _ = [||]Write().Length;
                    }

                    public string Write() => throw null;
                    public Task<string> WriteAsync() => throw null;
                }
                """)
              .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        _ = (await WriteAsync()).Length;
                    }
                
                    public string Write() => throw null;
                    public Task<string> WriteAsync() => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task FixerKeepsGenericArgument()
    {
        await CreateProjectBuilder()
            .WithSourceCode(
                @"using System.Threading.Tasks;
class Buz
{
    private static async Task Do()
    {
        [||]Bar.Foo<int>();
    }
}

class Bar
{
    public static T Foo<T>()
        => default;

    public static Task<T> FooAsync<T>()
        => Task.FromResult(default(T));
}")
            .ShouldFixCodeWith(
                @"using System.Threading.Tasks;
class Buz
{
    private static async Task Do()
    {
        await Bar.FooAsync<int>();
    }
}

class Bar
{
    public static T Foo<T>()
        => default;

    public static Task<T> FooAsync<T>()
        => Task.FromResult(default(T));
}")
            .ValidateAsync();
    }


    [Fact]
    public async Task Async_Wait_Int32_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        [||]Task.Delay(1).Wait(10);
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_Wait_CancellationToken_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System;
using System.Threading;
using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        [||]Task.Delay(1).Wait(CancellationToken.None);
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_Wait_TimeSpan_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System;
using System.Threading;
using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        [||]Task.Delay(1).Wait(TimeSpan.FromSeconds(1));
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_Wait_Int32_CancellationToken_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System;
using System.Threading;
using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        [||]Task.Delay(1).Wait(10, CancellationToken.None);
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
              .ShouldFixCodeWith(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        _ = await Task.FromResult(1);
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
              .ShouldFixCodeWith(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        await Task.Delay(1);
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_ThreadSleep_TimeSpan_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System;
using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        [||]System.Threading.Thread.Sleep(TimeSpan.FromMinutes(1));
    }
}")
              .ShouldFixCodeWith(@"
using System;
using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        await Task.Delay(TimeSpan.FromMinutes(1));
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
    public async Task ProcessWaitForExit_NET5()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net5_0)
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
    public async Task ProcessWaitForExit_NET6()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(@"
using System.Threading.Tasks;
using System.Diagnostics;

class Test
{
    public async Task A()
    {
        var process = new Process();
        [||]process.WaitForExit();
    }
}")
              .ShouldFixCodeWith(@"
using System.Threading.Tasks;
using System.Diagnostics;

class Test
{
    public async Task A()
    {
        var process = new Process();
        await process.WaitForExitAsync();
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
    public async Task Using_Diagnostic1()
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
    }

    private class Sample : IDisposable
    {
        public void Dispose() => throw null;
        public ValueTask DisposeAsync() => throw null;
    }
}")
              .ShouldBatchFixCodeWith(@"
using System;
using System.Threading.Tasks;
using System.Diagnostics;

class Test
{
    public async Task A()
    {
        await using var a = new Sample();
    }

    private class Sample : IDisposable
    {
        public void Dispose() => throw null;
        public ValueTask DisposeAsync() => throw null;
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Using_Diagnostic2()
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
        [||]using (var b = new Sample()) { }
    }

    private class Sample : IDisposable
    {
        public void Dispose() => throw null;
        public ValueTask DisposeAsync() => throw null;
    }
}")
              .ShouldBatchFixCodeWith(@"
using System;
using System.Threading.Tasks;
using System.Diagnostics;

class Test
{
    public async Task A()
    {
        await using (var b = new Sample()) { }
    }

    private class Sample : IDisposable
    {
        public void Dispose() => throw null;
        public ValueTask DisposeAsync() => throw null;
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Using_Diagnostic3()
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
        var sample = new Sample();
        [||]using (sample) { }
    }

    private class Sample : IDisposable
    {
        public void Dispose() => throw null;
        public ValueTask DisposeAsync() => throw null;
    }
}")
              .ShouldBatchFixCodeWith(@"
using System;
using System.Threading.Tasks;
using System.Diagnostics;

class Test
{
    public async Task A()
    {
        var sample = new Sample();
        await using (sample) { }
    }

    private class Sample : IDisposable
    {
        public void Dispose() => throw null;
        public ValueTask DisposeAsync() => throw null;
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Using_Diagnostic4()
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

    [Fact]
    public async Task ExtensionMethod()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

class Test
{
    public void A()
    {
    }
}

static class TestExtensions
{
    public static async Task AAsync(this Test test, CancellationToken token = default)
    {
    }
}

class demo
{
    public async Task a()
    {
        [||]new Test().A();
    }
}
")
              .ShouldFixCodeWith(@"
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

class Test
{
    public void A()
    {
    }
}

static class TestExtensions
{
    public static async Task AAsync(this Test test, CancellationToken token = default)
    {
    }
}

class demo
{
    public async Task a()
    {
        await new Test().AAsync();
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task CreateAsyncScope()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.AspNetCore6_0)
              .WithSourceCode(@"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

class demo
{
    public async Task a()
    {
        IServiceProvider provider = null;
        await using var scope1 = provider.CreateAsyncScope();
        using var scope2 = [||]provider.CreateScope();
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task CreateAsyncScope_net5()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.AspNetCore5_0)
              .WithSourceCode(@"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

class demo
{
    public async Task a()
    {
        IServiceProvider provider = null;
        using var scope = provider.CreateScope();
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task DbContext_Add()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .AddNuGetReference("Microsoft.EntityFrameworkCore", "6.0.8", "lib/net6.0/")
              .AddNuGetReference("Microsoft.EntityFrameworkCore.Abstractions", "6.0.8", "lib/net6.0/")
              .WithSourceCode(@"
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

class BloggingContext : DbContext
{
    public DbSet<object> Blogs { get; set; }
}

class Sample
{
    async Task A()
    {
        var context = new BloggingContext();
        context.Add(new());
        context.Blogs.Add(new());
        await context.Blogs.AddAsync(new());
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task IAsyncEnumerable()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode("""
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class demo
{
    public IAsyncEnumerable<int> A()
    {
        [||]Thread.Sleep(1);
        throw null;
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task IAsyncEnumerator()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode("""
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class demo
{
    public IAsyncEnumerator<int> A()
    {
        [||]Thread.Sleep(1);
        throw null;
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task AsyncMethodBuilder()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode("""
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

[System.Runtime.CompilerServices.AsyncMethodBuilderAttribute(typeof(int))]
class Sample
{
    public Sample A()
    {
        [||]Thread.Sleep(1);
        throw null;
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task TaskYieldResult()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode("""
using System;
using System.Threading;

class Sample
{
    public System.Runtime.CompilerServices.YieldAwaitable A()
    {
        Thread.Sleep(1);
        throw null;
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task TopLevelStatement()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.WindowsApplication)
              .WithSourceCode("""
[||]System.Threading.Thread.Sleep(1);
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task TaskRunDelegate()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode("""
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class Sample
{
    public void A()
    {
        _ = Task.Run(() => [||]Thread.Sleep(1));
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task Moq_Raise()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .AddNuGetReference("Moq", "4.20.0", "lib/net6.0/")
              .WithSourceCode("""
using System;
using Moq;

class Sample
{
    public void A()
    {
        new Mock<ICloneable>().Raise(null);
        _ = new Mock<ICloneable>().RaiseAsync(null);
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewMemoryStream()
    {
        await CreateProjectBuilder()
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using var ms = new System.IO.MemoryStream();
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task SemaphoreSlim_Wait_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        var semaphore = new SemaphoreSlim(1);
                        semaphore.Wait(0);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task SemaphoreSlim_Wait_TimeSpanZero_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System;
                using System.Threading;
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        var semaphore = new SemaphoreSlim(1);
                        semaphore.Wait(TimeSpan.Zero);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task SemaphoreSlim_Wait_NonZero_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        var semaphore = new SemaphoreSlim(1);
                        [||]semaphore.Wait(100);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task SemaphoreSlim_Wait_NoArgs_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        var semaphore = new SemaphoreSlim(1);
                        [||]semaphore.Wait();
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task SemaphoreSlim_Wait_ZeroWithCancellationToken_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        var semaphore = new SemaphoreSlim(1);
                        semaphore.Wait(0, CancellationToken.None);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task TemporaryDirectory_InTestProject_WithXunit_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .AddXUnitApi()
            .WithSourceCode("""
                using System.Threading.Tasks;
                using Meziantou.Framework;

                namespace Meziantou.Framework
                {
                    public class TemporaryDirectory
                    {
                        public void CreateTextFile(string path, string content) { }
                        public Task CreateTextFileAsync(string path, string content) => Task.CompletedTask;
                    }
                }

                class Test
                {
                    public async Task A()
                    {
                        var dir = new TemporaryDirectory();
                        dir.CreateTextFile("test.txt", "content");
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TemporaryDirectory_InTestProject_WithNUnit_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .AddNUnitApi()
            .WithSourceCode("""
                using System.Threading.Tasks;
                using Meziantou.Framework;

                namespace Meziantou.Framework
                {
                    public class TemporaryDirectory
                    {
                        public void CreateTextFile(string path, string content) { }
                        public Task CreateTextFileAsync(string path, string content) => Task.CompletedTask;
                    }
                }

                class Test
                {
                    public async Task A()
                    {
                        var dir = new TemporaryDirectory();
                        dir.CreateTextFile("test.txt", "content");
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TemporaryDirectory_InTestProject_WithMSTest_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .AddMSTestApi()
            .WithSourceCode("""
                using System.Threading.Tasks;
                using Meziantou.Framework;

                namespace Meziantou.Framework
                {
                    public class TemporaryDirectory
                    {
                        public void CreateTextFile(string path, string content) { }
                        public Task CreateTextFileAsync(string path, string content) => Task.CompletedTask;
                    }
                }

                class Test
                {
                    public async Task A()
                    {
                        var dir = new TemporaryDirectory();
                        dir.CreateTextFile("test.txt", "content");
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TemporaryDirectory_InNonTestProject_Diagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                using Meziantou.Framework;

                namespace Meziantou.Framework
                {
                    public class TemporaryDirectory
                    {
                        public void CreateTextFile(string path, string content) { }
                        public Task CreateTextFileAsync(string path, string content) => Task.CompletedTask;
                    }
                }

                class Test
                {
                    public async Task A()
                    {
                        var dir = new TemporaryDirectory();
                        [||]dir.CreateTextFile("test.txt", "content");
                    }
                }
                """)
            .ValidateAsync();
    }
}

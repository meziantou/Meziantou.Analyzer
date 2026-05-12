using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
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
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        [|Task.Delay(1).Wait()|];
                    }
                }
                """)
              .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        await Task.Delay(1);
                    }
                }
                """)
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
                        _ = [|Write()|].Length;
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
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Buz
                {
                    private static async Task Do()
                    {
                        [|Bar.Foo<int>()|];
                    }
                }

                class Bar
                {
                    public static T Foo<T>()
                        => default;

                    public static Task<T> FooAsync<T>()
                        => Task.FromResult(default(T));
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
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
                }
                """)
            .ValidateAsync();
    }


    [Fact]
    public async Task Async_Wait_Int32_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        [|Task.Delay(1).Wait(10)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_Wait_CancellationToken_Diagnostic()
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
                        [|Task.Delay(1).Wait(CancellationToken.None)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_Wait_TimeSpan_Diagnostic()
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
                        [|Task.Delay(1).Wait(TimeSpan.FromSeconds(1))|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_Wait_Int32_CancellationToken_Diagnostic()
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
                        [|Task.Delay(1).Wait(10, CancellationToken.None)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_Result_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        _ = [|Task.FromResult(1).Result|];
                    }
                }
                """)
              .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        _ = await Task.FromResult(1);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_ValueTask_Result_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        _ = [|new ValueTask<int>(10).Result|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_ValueTask_GetAwaiter_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        _ = [|new ValueTask<int>(10).GetAwaiter().GetResult()|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_ThreadSleep_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        [|System.Threading.Thread.Sleep(1)|];
                    }
                }
                """)
              .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        await Task.Delay(1);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_ThreadSleep_TimeSpan_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        [|System.Threading.Thread.Sleep(TimeSpan.FromMinutes(1))|];
                    }
                }
                """)
              .ShouldFixCodeWith("""
                using System;
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1));
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_SuggestOverload_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        [|Write()|];
                    }

                    public void Write() => throw null;
                    public Task Write(System.Threading.CancellationToken cancellationToken) => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_AsyncSuffix_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        [|Write()|];
                    }

                    public void Write() => throw null;
                    public Task WriteAsync() => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Async_NoOverload_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        Write();
                    }

                    public void Write() => throw null;
                    public void WriteAsync() => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AsyncLambda_Overload_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        System.Func<Task> a = async () => [|Write()|];
                    }

                    public void Write() => throw null;
                    public Task WriteAsync() => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AsyncLocalFunction_Overload_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public void A()
                    {
                        Local();

                        async Task Local() => [|Write()|];
                    }

                    public void Write() => throw null;
                    public Task WriteAsync() => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task AsyncLocalFunction_Overload_ValueTask_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public void A()
                    {
                        Local();

                        async Task Local() => [|Write()|];
                    }

                    public void Write() => throw null;
                    public ValueTask WriteAsync() => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/169")]
    public async Task AsyncMethodWithAsyncOverload()
    {
        await CreateProjectBuilder()
                .AddSystemTextJson()
                .WithSourceCode("""
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
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Method_NoOverload_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        Write();
                    }

                    public void Write() => throw null;
                    public void Write(System.Threading.CancellationToken cancellationToken) => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Method_NoOverloadWithSameParameters_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        Write();
                    }

                    public void Write() => throw null;
                    public Task Write(int a) => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Method_NonGenericOverloadWithGenericAwaitableOverload_NoDiagnostic()
    {
        // The non-generic method has a same-named generic overload with an awaitable return type.
        // The compiler always prefers the non-generic method, so adding await would still resolve
        // to the non-generic (non-awaitable) method, making the suggestion invalid (false positive).
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;
                class Test
                {
                    public async Task A()
                    {
                        var x = "hello";
                        Assert.That(x);
                    }
                }

                static class Assert
                {
                    public static ValueAssertion That(string? value) => throw null;
                    public static ValueAssertion<T> That<T>(T value) => throw null;
                }

                class ValueAssertion { }

                class ValueAssertion<T>
                {
                    public TaskAwaiter GetAwaiter() => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Console_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading.Tasks;
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
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ProcessWaitForExit_NET5()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net5_0)
              .WithSourceCode("""
                using System.Threading.Tasks;
                using System.Diagnostics;

                class Test
                {
                    public async Task A()
                    {
                        var process = new Process();
                        process.WaitForExit();
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ProcessWaitForExit_NET6()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode("""
                using System.Threading.Tasks;
                using System.Diagnostics;

                class Test
                {
                    public async Task A()
                    {
                        var process = new Process();
                        [|process.WaitForExit()|];
                    }
                }
                """)
              .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                using System.Diagnostics;

                class Test
                {
                    public async Task A()
                    {
                        var process = new Process();
                        await process.WaitForExitAsync();
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Using_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp9)
              .WithSourceCode("""
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
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Using_Diagnostic1()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                using System.Diagnostics;

                class Test
                {
                    public async Task A()
                    {
                        [|using var a = new Sample();|]
                    }

                    private class Sample : IDisposable
                    {
                        public void Dispose() => throw null;
                        public ValueTask DisposeAsync() => throw null;
                    }
                }
                """)
              .ShouldBatchFixCodeWith("""
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
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Using_Diagnostic1_WithComment()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        // MA0042 "Prefer using 'await using'"
                        [|using var a = new Sample();|]
                    }

                    private class Sample : IDisposable
                    {
                        public void Dispose() => throw null;
                        public ValueTask DisposeAsync() => throw null;
                    }
                }
                """)
              .ShouldBatchFixCodeWith("""
                using System;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        // MA0042 "Prefer using 'await using'"
                        await using var a = new Sample();
                    }

                    private class Sample : IDisposable
                    {
                        public void Dispose() => throw null;
                        public ValueTask DisposeAsync() => throw null;
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Using_Diagnostic2()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                using System.Diagnostics;

                class Test
                {
                    public async Task A()
                    {
                        [|using (var b = new Sample()) { }|]
                    }

                    private class Sample : IDisposable
                    {
                        public void Dispose() => throw null;
                        public ValueTask DisposeAsync() => throw null;
                    }
                }
                """)
              .ShouldBatchFixCodeWith("""
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
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Using_Diagnostic3()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                using System.Diagnostics;

                class Test
                {
                    public async Task A()
                    {
                        var sample = new Sample();
                        [|using (sample) { }|]
                    }

                    private class Sample : IDisposable
                    {
                        public void Dispose() => throw null;
                        public ValueTask DisposeAsync() => throw null;
                    }
                }
                """)
              .ShouldBatchFixCodeWith("""
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
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Using_Diagnostic4()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
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
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ExtensionMethod()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System;
                using System.Threading;
                using System.Threading.Tasks;
                using System.Diagnostics;

                class Test
                {
                    public void A() => throw null;
                }

                static class TestExtensions
                {
                    public static async Task AAsync(this Test test, CancellationToken token = default) => throw null;
                }

                class demo
                {
                    public async Task a()
                    {
                        [|new Test().A()|];
                    }
                }
                """)
              .ShouldFixCodeWith("""
                using System;
                using System.Threading;
                using System.Threading.Tasks;
                using System.Diagnostics;

                class Test
                {
                    public void A() => throw null;
                }

                static class TestExtensions
                {
                    public static async Task AAsync(this Test test, CancellationToken token = default) => throw null;
                }

                class demo
                {
                    public async Task a()
                    {
                        await new Test().AAsync();
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericArgument_MultipleIncompatibleGenericArguments_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(List<int> a, List<string> b) => throw null;
                    public Task AAsync<T>(List<T> a, List<T> b, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        new Test().A(new List<int>(), new List<string>());
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ExtensionMethod_GenericArgumentsIncompatible_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                }

                static class TestExtensions
                {
                    public static void A(this Test test, List<int> a, List<string> b) => throw null;
                    public static Task AAsync<T>(this Test test, List<T> a, List<T> b, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        new Test().A(new List<int>(), new List<string>());
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericArgument_ListToIEnumerable_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(List<int> value) => throw null;
                    public Task AAsync<T>(IEnumerable<T> value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        new Test().A(new List<int>());
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericArgument_NestedGenericIncompatibility_ShouldNotReport()
    {
        // For simplicity, we skip checking compatibility of nested generics
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(List<List<int>> value) => throw null;
                    public Task AAsync<T>(List<List<T>> value, CancellationToken token = default) => throw null;
                    public Task AAsync(List<List<string>> value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        new Test().A(new List<List<int>>());
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericArgument_SameOriginalDefinitionButDifferentTypeParameterMapping_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A<T1, T2>(Dictionary<T1, T2> value) => throw null;
                    public Task AAsync<T>(Dictionary<T, T> value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        new Test().A<int, string>(new Dictionary<int, string>());
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericArgument_SingleGenericArgument_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A<T>(List<T> value) => throw null;
                    public Task AAsync<T>(List<T> value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A<int>(new List<int>())|];
                    }
                }
                """)
              .ShouldFixCodeWith("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A<T>(List<T> value) => throw null;
                    public Task AAsync<T>(List<T> value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        await new Test().AAsync<int>(new List<int>());
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ExtensionMethod_GenericArgument_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                }

                static class TestExtensions
                {
                    public static void A<T>(this Test test, List<T> value) => throw null;
                    public static Task AAsync<T>(this Test test, List<T> value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A<int>(new List<int>())|];
                    }
                }
                """)
              .ShouldFixCodeWith("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                }

                static class TestExtensions
                {
                    public static void A<T>(this Test test, List<T> value) => throw null;
                    public static Task AAsync<T>(this Test test, List<T> value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        await new Test().AAsync<int>(new List<int>());
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericArgument_ArrayOfGenericArgument_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A<T>(T[] value) => throw null;
                    public Task AAsync<T>(T[] value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A<int>(new int[1])|];
                    }
                }
                """)
              .ShouldFixCodeWith("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A<T>(T[] value) => throw null;
                    public Task AAsync<T>(T[] value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        await new Test().AAsync<int>(new int[1]);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericArgument_AsyncConstraintIncompatible_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A<T>(List<T> value) => throw null;
                    public Task AAsync<T>(List<T> value, CancellationToken token = default)
                        where T : class => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        new Test().A<int>(new List<int>());
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_InModifierDifference_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(in int value) => throw null;
                    public Task AAsync(int value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        var value = 1;
                        [|new Test().A(in value)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_RefMismatch_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(ref int value) => throw null;
                    public Task AAsync(int value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        var value = 1;
                        new Test().A(ref value);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_OutMismatch_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(out int value)
                    {
                        value = 0;
                    }

                    public Task AAsync(int value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        new Test().A(out var value);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_NullLiteral_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(string value) => throw null;
                    public Task AAsync(string value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A(null)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_DefaultLiteral_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(string value) => throw null;
                    public Task AAsync(string value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A(default)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_ImplicitNumericConversion_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(long value) => throw null;
                    public Task AAsync(long value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A(42)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_ImplicitNumericWidening_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(int value) => throw null;
                    public Task AAsync(long value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A(1)|];
                    }
                }
                """)
              .ShouldFixCodeWith("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(int value) => throw null;
                    public Task AAsync(long value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        await new Test().AAsync(1);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_ImplicitNumericNarrowing_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(long value) => throw null;
                    public Task AAsync(int value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        new Test().A(1L);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_ImplicitNumericToFloatingPoint_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(int value) => throw null;
                    public Task AAsync(double value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A(1)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_ImplicitNumericFloatingPointToInteger_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(double value) => throw null;
                    public Task AAsync(int value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        new Test().A(1.0);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_ImplicitNumericInt64ToFloatingPoint_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(long value) => throw null;
                    public Task AAsync(double value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        new Test().A(1L);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_ImplicitNumericInt32ToFloatingPoint_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(int value) => throw null;
                    public Task AAsync(float value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        new Test().A(1);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_ImplicitNumericByteToInt32_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(byte value) => throw null;
                    public Task AAsync(int value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A((byte)1)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_ImplicitNumericInt16ToInt32_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(short value) => throw null;
                    public Task AAsync(int value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A((short)1)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_ImplicitNumericSingleToDouble_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(float value) => throw null;
                    public Task AAsync(double value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A(1f)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_ImplicitNumericHalfToSingle_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode("""
                using System;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(Half value) => throw null;
                    public Task AAsync(float value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A((Half)1)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_ImplicitNumericHalfToDouble_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode("""
                using System;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(Half value) => throw null;
                    public Task AAsync(double value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A((Half)1)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericArgument_CompatibleGenericDefinitions_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(List<int> value) => throw null;
                    public Task AAsync<T>(IReadOnlyCollection<T> value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A(new List<int>())|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericArgument_DifferentArity_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(Dictionary<int, string> value) => throw null;
                    public Task AAsync<T>(List<T> value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        new Test().A(new Dictionary<int, string>());
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericArgument_ConstraintNewIncompatible_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                class WithoutPublicParameterlessConstructor
                {
                    public WithoutPublicParameterlessConstructor(int value)
                    {
                    }
                }

                class Test
                {
                    public void A<T>(List<T> value) => throw null;
                    public Task AAsync<T>(List<T> value, CancellationToken token = default)
                        where T : new() => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        new Test().A<WithoutPublicParameterlessConstructor>(new List<WithoutPublicParameterlessConstructor>());
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericArgument_DifferentTypeConstraintsOrder_ShouldNotReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                interface IMark1 { }
                interface IMark2 { }

                class Mark : IMark1, IMark2 { }

                class Test
                {
                    public void A<T>(int i, List<T> test) where T : IMark1, IMark2 => throw null;
                    public Task AAsync<T>(int i, List<T> test, CancellationToken token = default) where T : IMark2, IMark1 => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        new Test().A<Mark>(1, new List<Mark>());
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericMethod_SameConstraints_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;

                interface IMark1 { }
                interface IMark2 { }

                class Mark : IMark1, IMark2 { }

                class Test
                {
                    public void A<T>(int i, List<T> test) where T : class, IMark1, IMark2 => throw null;
                    public Task AAsync<T>(int i, List<T> test, CancellationToken token = default) where T : class, IMark1, IMark2 => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A<Mark>(1, new List<Mark>())|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_ImplicitUserDefinedConversion_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Value
                {
                    public static implicit operator Value(string value) => new Value();
                }

                class Test
                {
                    public void A(Value value) => throw null;
                    public Task AAsync(Value value, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A("value")|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ExtensionMethodToInstanceMethod_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public Task AAsync(int value, CancellationToken token = default) => throw null;
                }

                static class TestExtensions
                {
                    public static void A(this Test test, int value) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A(1)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Argument_NamedArgumentsReordered_ShouldReport()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public void A(int left, int right) => throw null;
                    public Task AAsync(int left, int right, CancellationToken token = default) => throw null;
                }

                class Demo
                {
                    public async Task M()
                    {
                        [|new Test().A(right: 2, left: 1)|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CreateAsyncScope()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.AspNetCore6_0)
              .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                using Microsoft.Extensions.DependencyInjection;

                class demo
                {
                    public async Task a()
                    {
                        IServiceProvider provider = null;
                        await using var scope1 = provider.CreateAsyncScope();
                        using var scope2 = [|provider.CreateScope()|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CreateAsyncScope_net5()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.AspNetCore5_0)
              .WithSourceCode("""
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
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task DbContext_Add()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .AddNuGetReference("Microsoft.EntityFrameworkCore", "6.0.8", "lib/net6.0/")
              .AddNuGetReference("Microsoft.EntityFrameworkCore.Abstractions", "6.0.8", "lib/net6.0/")
              .WithSourceCode("""
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
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/891")]
    public async Task IDbContextFactory_CreateDbContext_NoReport()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .AddNuGetReference("Microsoft.EntityFrameworkCore", "6.0.8", "lib/net6.0/")
              .AddNuGetReference("Microsoft.EntityFrameworkCore.Abstractions", "6.0.8", "lib/net6.0/")
              .WithSourceCode("""
                using System.Threading.Tasks;
                using Microsoft.EntityFrameworkCore;

                class BloggingContext : DbContext { }

                class Sample
                {
                    private IDbContextFactory<BloggingContext> _factory;

                    async Task A()
                    {
                        await using var context = _factory.CreateDbContext();
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task SqliteConnection_CreateCommand_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .WithSourceCode("""
                using System.Threading.Tasks;
                using Microsoft.Data.Sqlite;

                class Test
                {
                    public async Task A(SqliteConnection connection)
                    {
                        using var command = connection.CreateCommand();
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task SqliteConnection_Close_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .WithSourceCode("""
                using System.Threading.Tasks;
                using Microsoft.Data.Sqlite;

                class Test
                {
                    public async Task A(SqliteConnection connection)
                    {
                        connection.Close();
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task SqliteCommand_Prepare_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .WithSourceCode("""
                using System.Threading.Tasks;
                using Microsoft.Data.Sqlite;

                class Test
                {
                    public async Task A(SqliteCommand command)
                    {
                        command.Prepare();
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task SqliteConnection_CreateCommand_OptionDisabled_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .AddAnalyzerConfiguration("MA0042.enable_sqlite_special_cases", "false")
              .WithSourceCode("""
                using System.Threading.Tasks;
                using Microsoft.Data.Sqlite;

                class Test
                {
                    public async Task A(SqliteConnection connection)
                    {
                        [|using var command = connection.CreateCommand();|]
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task SqliteConnection_Close_OptionDisabled_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .AddAnalyzerConfiguration("MA0042.enable_sqlite_special_cases", "false")
              .WithSourceCode("""
                using System.Threading.Tasks;
                using Microsoft.Data.Sqlite;

                class Test
                {
                    public async Task A(SqliteConnection connection)
                    {
                        [|connection.Close()|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task SqliteCommand_Prepare_OptionDisabled_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .AddAnalyzerConfiguration("MA0042.enable_sqlite_special_cases", "false")
              .WithSourceCode("""
                using System.Threading.Tasks;
                using Microsoft.Data.Sqlite;

                class Test
                {
                    public async Task A(SqliteCommand command)
                    {
                        [|command.Prepare()|];
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task SqliteDataReader_Read_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .WithSourceCode("""
                using System.Threading.Tasks;
                using Microsoft.Data.Sqlite;

                class Test
                {
                    public async Task A(SqliteDataReader reader)
                    {
                        reader.Read();
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public async Task SqliteDataReader_Read_OptionDisabled_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddNuGetReference("Microsoft.Data.Sqlite.Core", "8.0.0", "lib/net8.0/")
              .AddAnalyzerConfiguration("MA0042.enable_sqlite_special_cases", "false")
              .WithSourceCode("""
                using System.Threading.Tasks;
                using Microsoft.Data.Sqlite;

                class Test
                {
                    public async Task A(SqliteDataReader reader)
                    {
                        [|reader.Read()|];
                    }
                }
                """)
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
        [|Thread.Sleep(1)|];
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
        [|Thread.Sleep(1)|];
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
        [|Thread.Sleep(1)|];
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
[|System.Threading.Thread.Sleep(1)|];
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
        _ = Task.Run(() => [|Thread.Sleep(1)|]);
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
    public async Task UsingFactoryMethod_StreamSubclass_NoDisposeAsyncOverride_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.IO;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var s = CreateStream();|]
                    }

                    private MyStream CreateStream() => throw null;
                }

                class MyStream : Stream
                {
                    public override bool CanRead => throw null;
                    public override bool CanSeek => throw null;
                    public override bool CanWrite => throw null;
                    public override long Length => throw null;
                    public override long Position { get => throw null; set => throw null; }
                    public override void Flush() => throw null;
                    public override int Read(byte[] buffer, int offset, int count) => throw null;
                    public override long Seek(long offset, SeekOrigin origin) => throw null;
                    public override void SetLength(long value) => throw null;
                    public override void Write(byte[] buffer, int offset, int count) => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewStreamSubclass_WithDisposeAsyncOverride_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.IO;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var s = new MyStream();|]
                    }
                }

                class MyStream : Stream
                {
                    public override bool CanRead => throw null;
                    public override bool CanSeek => throw null;
                    public override bool CanWrite => throw null;
                    public override long Length => throw null;
                    public override long Position { get => throw null; set => throw null; }
                    public override void Flush() => throw null;
                    public override int Read(byte[] buffer, int offset, int count) => throw null;
                    public override long Seek(long offset, SeekOrigin origin) => throw null;
                    public override void SetLength(long value) => throw null;
                    public override void Write(byte[] buffer, int offset, int count) => throw null;
                    public override ValueTask DisposeAsync() => throw null;
                }
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
                        [|semaphore.Wait(100)|];
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
                        [|semaphore.Wait()|];
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
                        [|dir.CreateTextFile("test.txt", "content")|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewDbConnectionSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        using var conn = new MySqlConnection();
                    }
                }

                class MySqlConnection : DbConnection
                {
                    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw null;
                    protected override DbCommand CreateDbCommand() => throw null;
                    public override void ChangeDatabase(string databaseName) => throw null;
                    public override void Close() => throw null;
                    public override void Open() => throw null;
                    public override string ConnectionString { get => throw null; set => throw null; }
                    public override string Database => throw null;
                    public override string DataSource => throw null;
                    public override string ServerVersion => throw null;
                    public override ConnectionState State => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingFactoryMethod_DbConnectionSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        using var conn = CreateConnection();
                    }

                    private MySqlConnection CreateConnection() => throw null;
                }

                class MySqlConnection : DbConnection
                {
                    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw null;
                    protected override DbCommand CreateDbCommand() => throw null;
                    public override void ChangeDatabase(string databaseName) => throw null;
                    public override void Close() => throw null;
                    public override void Open() => throw null;
                    public override string ConnectionString { get => throw null; set => throw null; }
                    public override string Database => throw null;
                    public override string DataSource => throw null;
                    public override string ServerVersion => throw null;
                    public override ConnectionState State => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingFactoryMethod_DbConnectionSubclass_NoDisposeAsyncOverride_OptionDisabled_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddAnalyzerConfiguration("MA0042.enable_db_special_cases", "false")
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var conn = CreateConnection();|]
                    }

                    private MySqlConnection CreateConnection() => throw null;
                }

                class MySqlConnection : DbConnection
                {
                    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw null;
                    protected override DbCommand CreateDbCommand() => throw null;
                    public override void ChangeDatabase(string databaseName) => throw null;
                    public override void Close() => throw null;
                    public override void Open() => throw null;
                    public override string ConnectionString { get => throw null; set => throw null; }
                    public override string Database => throw null;
                    public override string DataSource => throw null;
                    public override string ServerVersion => throw null;
                    public override ConnectionState State => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewDbConnectionSubclass_WithDisposeAsyncOverride_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var conn = new MySqlConnection();|]
                    }
                }

                class MySqlConnection : DbConnection
                {
                    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw null;
                    protected override DbCommand CreateDbCommand() => throw null;
                    public override void ChangeDatabase(string databaseName) => throw null;
                    public override void Close() => throw null;
                    public override void Open() => throw null;
                    public override string ConnectionString { get => throw null; set => throw null; }
                    public override string Database => throw null;
                    public override string DataSource => throw null;
                    public override string ServerVersion => throw null;
                    public override ConnectionState State => throw null;
                    public override ValueTask DisposeAsync() => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewDbConnectionSubclass_DisposeAsyncOverriddenInIntermediateBase_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var conn = new DerivedConnection();|]
                    }
                }

                class BaseConnection : DbConnection
                {
                    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw null;
                    protected override DbCommand CreateDbCommand() => throw null;
                    public override void ChangeDatabase(string databaseName) => throw null;
                    public override void Close() => throw null;
                    public override void Open() => throw null;
                    public override string ConnectionString { get => throw null; set => throw null; }
                    public override string Database => throw null;
                    public override string DataSource => throw null;
                    public override string ServerVersion => throw null;
                    public override ConnectionState State => throw null;
                    public override ValueTask DisposeAsync() => throw null;
                }

                class DerivedConnection : BaseConnection { }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewDbCommandSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        using var command = new MyDbCommand();
                    }
                }

                class MyDbCommand : DbCommand
                {
                    public override string CommandText { get => throw null; set => throw null; }
                    public override int CommandTimeout { get => throw null; set => throw null; }
                    public override CommandType CommandType { get => throw null; set => throw null; }
                    public override bool DesignTimeVisible { get => throw null; set => throw null; }
                    public override UpdateRowSource UpdatedRowSource { get => throw null; set => throw null; }
                    protected override DbConnection DbConnection { get => throw null; set => throw null; }
                    protected override DbParameterCollection DbParameterCollection => throw null;
                    protected override DbTransaction DbTransaction { get => throw null; set => throw null; }
                    public override void Cancel() => throw null;
                    public override int ExecuteNonQuery() => throw null;
                    public override object ExecuteScalar() => throw null;
                    public override void Prepare() => throw null;
                    protected override DbParameter CreateDbParameter() => throw null;
                    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingFactoryMethod_DbCommandSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        using var command = CreateCommand();
                    }

                    private MyDbCommand CreateCommand() => throw null;
                }

                class MyDbCommand : DbCommand
                {
                    public override string CommandText { get => throw null; set => throw null; }
                    public override int CommandTimeout { get => throw null; set => throw null; }
                    public override CommandType CommandType { get => throw null; set => throw null; }
                    public override bool DesignTimeVisible { get => throw null; set => throw null; }
                    public override UpdateRowSource UpdatedRowSource { get => throw null; set => throw null; }
                    protected override DbConnection DbConnection { get => throw null; set => throw null; }
                    protected override DbParameterCollection DbParameterCollection => throw null;
                    protected override DbTransaction DbTransaction { get => throw null; set => throw null; }
                    public override void Cancel() => throw null;
                    public override int ExecuteNonQuery() => throw null;
                    public override object ExecuteScalar() => throw null;
                    public override void Prepare() => throw null;
                    protected override DbParameter CreateDbParameter() => throw null;
                    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingFactoryMethod_DbCommandSubclass_NoDisposeAsyncOverride_OptionDisabled_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddAnalyzerConfiguration("MA0042.enable_db_special_cases", "false")
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var command = CreateCommand();|]
                    }

                    private MyDbCommand CreateCommand() => throw null;
                }

                class MyDbCommand : DbCommand
                {
                    public override string CommandText { get => throw null; set => throw null; }
                    public override int CommandTimeout { get => throw null; set => throw null; }
                    public override CommandType CommandType { get => throw null; set => throw null; }
                    public override bool DesignTimeVisible { get => throw null; set => throw null; }
                    public override UpdateRowSource UpdatedRowSource { get => throw null; set => throw null; }
                    protected override DbConnection DbConnection { get => throw null; set => throw null; }
                    protected override DbParameterCollection DbParameterCollection => throw null;
                    protected override DbTransaction DbTransaction { get => throw null; set => throw null; }
                    public override void Cancel() => throw null;
                    public override int ExecuteNonQuery() => throw null;
                    public override object ExecuteScalar() => throw null;
                    public override void Prepare() => throw null;
                    protected override DbParameter CreateDbParameter() => throw null;
                    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewDbCommandSubclass_WithDisposeAsyncOverride_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var command = new MyDbCommand();|]
                    }
                }

                class MyDbCommand : DbCommand
                {
                    public override string CommandText { get => throw null; set => throw null; }
                    public override int CommandTimeout { get => throw null; set => throw null; }
                    public override CommandType CommandType { get => throw null; set => throw null; }
                    public override bool DesignTimeVisible { get => throw null; set => throw null; }
                    public override UpdateRowSource UpdatedRowSource { get => throw null; set => throw null; }
                    protected override DbConnection DbConnection { get => throw null; set => throw null; }
                    protected override DbParameterCollection DbParameterCollection => throw null;
                    protected override DbTransaction DbTransaction { get => throw null; set => throw null; }
                    public override void Cancel() => throw null;
                    public override int ExecuteNonQuery() => throw null;
                    public override object ExecuteScalar() => throw null;
                    public override void Prepare() => throw null;
                    protected override DbParameter CreateDbParameter() => throw null;
                    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw null;
                    public override ValueTask DisposeAsync() => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewDbCommandSubclass_DisposeAsyncOverriddenInIntermediateBase_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var command = new DerivedDbCommand();|]
                    }
                }

                class BaseDbCommand : DbCommand
                {
                    public override string CommandText { get => throw null; set => throw null; }
                    public override int CommandTimeout { get => throw null; set => throw null; }
                    public override CommandType CommandType { get => throw null; set => throw null; }
                    public override bool DesignTimeVisible { get => throw null; set => throw null; }
                    public override UpdateRowSource UpdatedRowSource { get => throw null; set => throw null; }
                    protected override DbConnection DbConnection { get => throw null; set => throw null; }
                    protected override DbParameterCollection DbParameterCollection => throw null;
                    protected override DbTransaction DbTransaction { get => throw null; set => throw null; }
                    public override void Cancel() => throw null;
                    public override int ExecuteNonQuery() => throw null;
                    public override object ExecuteScalar() => throw null;
                    public override void Prepare() => throw null;
                    protected override DbParameter CreateDbParameter() => throw null;
                    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw null;
                    public override ValueTask DisposeAsync() => throw null;
                }

                class DerivedDbCommand : BaseDbCommand { }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewDbDataReaderSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        using var reader = new DataTableReader(new DataTable());
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingFactoryMethod_DbDataReaderSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        using var reader = CreateReader();
                    }

                    private DataTableReader CreateReader() => new DataTableReader(new DataTable());
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingFactoryMethod_DbDataReaderSubclass_NoDisposeAsyncOverride_OptionDisabled_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddAnalyzerConfiguration("MA0042.enable_db_special_cases", "false")
              .WithSourceCode("""
                using System.Data;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var reader = CreateReader();|]
                    }

                    private DataTableReader CreateReader() => new DataTableReader(new DataTable());
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewDbDataReaderSubclass_WithDisposeAsyncOverride_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System;
                using System.Collections;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var reader1 = new MyDbDataReader();|]
                        [|using var reader2 = new DerivedDbDataReader();|]
                    }
                }

                class MyDbDataReader : DbDataReader
                {
                    public override object this[int ordinal] => throw null;
                    public override object this[string name] => throw null;
                    public override int Depth => throw null;
                    public override int FieldCount => throw null;
                    public override bool HasRows => throw null;
                    public override bool IsClosed => throw null;
                    public override int RecordsAffected => throw null;
                    public override bool GetBoolean(int ordinal) => throw null;
                    public override byte GetByte(int ordinal) => throw null;
                    public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => throw null;
                    public override char GetChar(int ordinal) => throw null;
                    public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => throw null;
                    public override string GetDataTypeName(int ordinal) => throw null;
                    public override DateTime GetDateTime(int ordinal) => throw null;
                    public override decimal GetDecimal(int ordinal) => throw null;
                    public override double GetDouble(int ordinal) => throw null;
                    public override IEnumerator GetEnumerator() => throw null;
                    public override Type GetFieldType(int ordinal) => throw null;
                    public override float GetFloat(int ordinal) => throw null;
                    public override Guid GetGuid(int ordinal) => throw null;
                    public override short GetInt16(int ordinal) => throw null;
                    public override int GetInt32(int ordinal) => throw null;
                    public override long GetInt64(int ordinal) => throw null;
                    public override string GetName(int ordinal) => throw null;
                    public override int GetOrdinal(string name) => throw null;
                    public override string GetString(int ordinal) => throw null;
                    public override object GetValue(int ordinal) => throw null;
                    public override int GetValues(object[] values) => throw null;
                    public override bool IsDBNull(int ordinal) => throw null;
                    public override bool NextResult() => throw null;
                    public override bool Read() => throw null;
                    public override ValueTask DisposeAsync() => throw null;
                }

                class DerivedDbDataReader : MyDbDataReader { }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewDbTransactionSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        using var transaction = new MyDbTransaction();
                    }
                }

                class MyDbTransaction : DbTransaction
                {
                    protected override DbConnection DbConnection => throw null;
                    public override IsolationLevel IsolationLevel => throw null;
                    public override void Commit() => throw null;
                    public override void Rollback() => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingFactoryMethod_DbTransactionSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        using var transaction = CreateTransaction();
                    }

                    private MyDbTransaction CreateTransaction() => throw null;
                }

                class MyDbTransaction : DbTransaction
                {
                    protected override DbConnection DbConnection => throw null;
                    public override IsolationLevel IsolationLevel => throw null;
                    public override void Commit() => throw null;
                    public override void Rollback() => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingFactoryMethod_DbTransactionSubclass_NoDisposeAsyncOverride_OptionDisabled_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddAnalyzerConfiguration("MA0042.enable_db_special_cases", "false")
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var transaction = CreateTransaction();|]
                    }

                    private MyDbTransaction CreateTransaction() => throw null;
                }

                class MyDbTransaction : DbTransaction
                {
                    protected override DbConnection DbConnection => throw null;
                    public override IsolationLevel IsolationLevel => throw null;
                    public override void Commit() => throw null;
                    public override void Rollback() => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewDbTransactionSubclass_WithDisposeAsyncOverride_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var transaction1 = new MyDbTransaction();|]
                        [|using var transaction2 = new DerivedDbTransaction();|]
                    }
                }

                class MyDbTransaction : DbTransaction
                {
                    protected override DbConnection DbConnection => throw null;
                    public override IsolationLevel IsolationLevel => throw null;
                    public override void Commit() => throw null;
                    public override void Rollback() => throw null;
                    public override ValueTask DisposeAsync() => throw null;
                }

                class DerivedDbTransaction : MyDbTransaction { }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewDbBatchSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        using var batch = new MyDbBatch();
                    }
                }

                class MyDbBatch : DbBatch
                {
                    public override int Timeout { get => throw null; set => throw null; }
                    protected override DbBatchCommandCollection DbBatchCommands => throw null;
                    protected override DbConnection DbConnection { get => throw null; set => throw null; }
                    protected override DbTransaction DbTransaction { get => throw null; set => throw null; }
                    public override void Cancel() => throw null;
                    protected override DbBatchCommand CreateDbBatchCommand() => throw null;
                    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw null;
                    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) => throw null;
                    public override int ExecuteNonQuery() => throw null;
                    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default) => throw null;
                    public override object ExecuteScalar() => throw null;
                    public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken = default) => throw null;
                    public override void Prepare() => throw null;
                    public override Task PrepareAsync(CancellationToken cancellationToken = default) => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingFactoryMethod_DbBatchSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        using var batch = CreateBatch();
                    }

                    private MyDbBatch CreateBatch() => throw null;
                }

                class MyDbBatch : DbBatch
                {
                    public override int Timeout { get => throw null; set => throw null; }
                    protected override DbBatchCommandCollection DbBatchCommands => throw null;
                    protected override DbConnection DbConnection { get => throw null; set => throw null; }
                    protected override DbTransaction DbTransaction { get => throw null; set => throw null; }
                    public override void Cancel() => throw null;
                    protected override DbBatchCommand CreateDbBatchCommand() => throw null;
                    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw null;
                    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) => throw null;
                    public override int ExecuteNonQuery() => throw null;
                    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default) => throw null;
                    public override object ExecuteScalar() => throw null;
                    public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken = default) => throw null;
                    public override void Prepare() => throw null;
                    public override Task PrepareAsync(CancellationToken cancellationToken = default) => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingFactoryMethod_DbBatchSubclass_NoDisposeAsyncOverride_OptionDisabled_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .AddAnalyzerConfiguration("MA0042.enable_db_special_cases", "false")
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var batch = CreateBatch();|]
                    }

                    private MyDbBatch CreateBatch() => throw null;
                }

                class MyDbBatch : DbBatch
                {
                    public override int Timeout { get => throw null; set => throw null; }
                    protected override DbBatchCommandCollection DbBatchCommands => throw null;
                    protected override DbConnection DbConnection { get => throw null; set => throw null; }
                    protected override DbTransaction DbTransaction { get => throw null; set => throw null; }
                    public override void Cancel() => throw null;
                    protected override DbBatchCommand CreateDbBatchCommand() => throw null;
                    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw null;
                    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) => throw null;
                    public override int ExecuteNonQuery() => throw null;
                    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default) => throw null;
                    public override object ExecuteScalar() => throw null;
                    public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken = default) => throw null;
                    public override void Prepare() => throw null;
                    public override Task PrepareAsync(CancellationToken cancellationToken = default) => throw null;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewDbBatchSubclass_WithDisposeAsyncOverride_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.Data;
                using System.Data.Common;
                using System.Threading;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var batch1 = new MyDbBatch();|]
                        [|using var batch2 = new DerivedDbBatch();|]
                    }
                }

                class MyDbBatch : DbBatch
                {
                    public override int Timeout { get => throw null; set => throw null; }
                    protected override DbBatchCommandCollection DbBatchCommands => throw null;
                    protected override DbConnection DbConnection { get => throw null; set => throw null; }
                    protected override DbTransaction DbTransaction { get => throw null; set => throw null; }
                    public override void Cancel() => throw null;
                    protected override DbBatchCommand CreateDbBatchCommand() => throw null;
                    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw null;
                    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) => throw null;
                    public override int ExecuteNonQuery() => throw null;
                    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default) => throw null;
                    public override object ExecuteScalar() => throw null;
                    public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken = default) => throw null;
                    public override void Prepare() => throw null;
                    public override Task PrepareAsync(CancellationToken cancellationToken = default) => throw null;
                    public override ValueTask DisposeAsync() => throw null;
                }

                class DerivedDbBatch : MyDbBatch { }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewTextWriterSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.IO;
                using System.Text;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        using var writer = new MyTextWriter();
                    }
                }

                class MyTextWriter : TextWriter
                {
                    public override Encoding Encoding => Encoding.UTF8;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingFactoryMethod_TextWriterSubclass_NoDisposeAsyncOverride_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.IO;
                using System.Text;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var writer = CreateTextWriter();|]
                    }

                    private MyTextWriter CreateTextWriter() => new MyTextWriter();
                }

                class MyTextWriter : TextWriter
                {
                    public override Encoding Encoding => Encoding.UTF8;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task UsingNewTextWriterSubclass_WithDisposeAsyncOverride_Diagnostic()
    {
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net8_0)
              .WithSourceCode("""
                using System.IO;
                using System.Text;
                using System.Threading.Tasks;

                class Test
                {
                    public async Task A()
                    {
                        [|using var writer1 = new MyTextWriter();|]
                        [|using var writer2 = new DerivedTextWriter();|]
                    }
                }

                class MyTextWriter : TextWriter
                {
                    public override Encoding Encoding => Encoding.UTF8;
                    public override ValueTask DisposeAsync() => throw null;
                }

                class DerivedTextWriter : MyTextWriter { }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ExcludeFromBlockingCallAnalysisAttribute_DocumentationIdMethod()
    {
        await CreateProjectBuilder()
              .AddMeziantouAttributes()
              .WithSourceCode("""
                using System.Threading.Tasks;
                [assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute("M:System.Threading.Tasks.Task.Wait")]

                class Test
                {
                    public async Task A()
                    {
                        Task.Delay(1).Wait();
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ExcludeFromBlockingCallAnalysisAttribute_DocumentationIdProperty()
    {
        await CreateProjectBuilder()
              .AddMeziantouAttributes()
              .WithSourceCode("""
                using System.Threading.Tasks;
                [assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute("P:System.Threading.Tasks.Task`1.Result")]

                class Test
                {
                    public async Task A()
                    {
                        _ = Task.FromResult(1).Result;
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ExcludeFromBlockingCallAnalysisAttribute_DoesNotAffectAwaitUsing()
    {
        await CreateProjectBuilder()
              .AddMeziantouAttributes()
              .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                [assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute("M:System.Threading.Tasks.Task.Wait")]

                class Test
                {
                    public async Task A()
                    {
                        [|using var value = new AsyncDisposable();|]
                    }
                }

                class AsyncDisposable : IDisposable, IAsyncDisposable
                {
                    public void Dispose() { }
                    public ValueTask DisposeAsync() => default;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ExcludeFromBlockingCallAnalysisAttribute_MethodSignature_AffectsAwaitUsing()
    {
        await CreateProjectBuilder()
              .AddMeziantouAttributes()
              .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                [assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute(typeof(Test), "Create")]

                class Test
                {
                    public async Task A()
                    {
                        using var value = Create();
                    }

                    private AsyncDisposable Create() => throw null;
                }

                class AsyncDisposable : IDisposable, IAsyncDisposable
                {
                    public void Dispose() { }
                    public ValueTask DisposeAsync() => default;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NonAwaitableTypeAttribute_DoesAffectAwaitUsing()
    {
        await CreateProjectBuilder()
              .AddMeziantouAttributes()
              .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(AsyncDisposable))]

                class Test
                {
                    public async Task A()
                    {
                        using var value = new AsyncDisposable();
                    }
                }

                class AsyncDisposable : IDisposable, IAsyncDisposable
                {
                    public void Dispose() { }
                    public ValueTask DisposeAsync() => default;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NonAwaitableTypeAttribute_DoesAffectAwaitSuggestion()
    {
        await CreateProjectBuilder()
              .AddMeziantouAttributes()
              .WithSourceCode("""
                using System.Threading.Tasks;
                [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(AwaitResult))]

                class Test
                {
                    public async Task A()
                    {
                        Create();
                    }

                    private AwaitResult Create() => throw null;
                    private Task<AwaitResult> CreateAsync() => throw null;
                }

                class AwaitResult { }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NonAwaitableTypeAttribute_DoesNotAffectOtherTypes()
    {
        await CreateProjectBuilder()
              .AddMeziantouAttributes()
              .WithSourceCode("""
                using System.Threading.Tasks;
                [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(OtherResult))]

                class Test
                {
                    public async Task A()
                    {
                        [|Create()|];
                    }

                    private AwaitResult Create() => throw null;
                    private Task<AwaitResult> CreateAsync() => throw null;
                }

                class AwaitResult { }
                class OtherResult { }
                """)
              .ValidateAsync();
    }
}

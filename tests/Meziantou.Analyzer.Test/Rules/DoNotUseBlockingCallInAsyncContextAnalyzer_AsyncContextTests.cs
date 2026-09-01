using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.DoNotUseBlockingCallInAsyncContextAnalyzer,
    Meziantou.Analyzer.Rules.DoNotUseBlockingCallInAsyncContextFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseBlockingCallInAsyncContextAnalyzer_AsyncContextTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add("MA0045");
        return test;
    }

    [Fact]
    public Task SemaphoreSlim_Wait_FieldTimeoutFromOtherSyntaxTree()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading;
            using System.Threading.Tasks;

            partial class Test
            {
                public async Task A(SemaphoreSlim semaphore)
                {
                    semaphore.Wait(Timeout);
                }
            }
            """;
        test.TestState.Sources.Add("""
            partial class Test
            {
                private readonly int Timeout = 0;
            }
            """);

        return test.RunAsync();
    }

    [Fact]
    public Task Async_Wait_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    {|MA0042:Task.Delay(1).Wait()|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    await Task.Delay(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FixerShouldAddParentheses()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    _ = {|MA0042:Write()|}.Length;
                }

                public string Write() => throw null;
                public Task<string> WriteAsync() => throw null;
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FixerKeepsGenericArgument()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Buz
            {
                private static async Task Do()
                {
                    {|MA0042:Bar.Foo<int>()|};
                }
            }

            class Bar
            {
                public static T Foo<T>()
                    => default;

                public static Task<T> FooAsync<T>()
                    => Task.FromResult(default(T));
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Async_Wait_Int32_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    {|MA0042:Task.Delay(1).Wait(10)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Async_Wait_CancellationToken_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    {|MA0042:Task.Delay(1).Wait(CancellationToken.None)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Async_Wait_TimeSpan_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    {|MA0042:Task.Delay(1).Wait(TimeSpan.FromSeconds(1))|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Async_Wait_Int32_CancellationToken_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    {|MA0042:Task.Delay(1).Wait(10, CancellationToken.None)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Async_Result_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    _ = {|MA0042:Task.FromResult(1).Result|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    _ = await Task.FromResult(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Async_ValueTask_Result_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    _ = {|MA0042:new ValueTask<int>(10).Result|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Async_ValueTask_GetAwaiter_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    _ = {|MA0042:new ValueTask<int>(10).GetAwaiter().GetResult()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Async_ThreadSleep_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    {|MA0042:System.Threading.Thread.Sleep(1)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    await Task.Delay(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Async_ThreadSleep_TimeSpan_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    {|MA0042:System.Threading.Thread.Sleep(TimeSpan.FromMinutes(1))|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Async_SuggestOverload_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    {|MA0042:Write()|};
                }

                public void Write() => throw null;
                public Task Write(System.Threading.CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Async_AsyncSuffix_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    {|MA0042:Write()|};
                }

                public void Write() => throw null;
                public Task WriteAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Async_NoOverload_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AsyncLambda_Overload_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    System.Func<Task> a = async () => {|MA0042:Write()|};
                }

                public void Write() => throw null;
                public Task WriteAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AsyncLocalFunction_Overload_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                public void A()
                {
                    Local();

                    async Task Local() => {|MA0042:Write()|};
                }

                public void Write() => throw null;
                public Task WriteAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AsyncLocalFunction_Overload_ValueTask_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                public void A()
                {
                    Local();

                    async Task Local() => {|MA0042:Write()|};
                }

                public void Write() => throw null;
                public ValueTask WriteAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/169")]
    public Task AsyncMethodWithAsyncOverload()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Method_NoOverload_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Method_NoOverloadWithSameParameters_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Method_NonGenericOverloadWithGenericAwaitableOverload_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Console_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ProcessWaitForExit_NET5()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net50;
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ProcessWaitForExit_NET6()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using System.Diagnostics;

            class Test
            {
                public async Task A()
                {
                    var process = new Process();
                    {|MA0042:process.WaitForExit()|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Using_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Using_Diagnostic1()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            using System.Diagnostics;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var a = new Sample();|}
                }

                private class Sample : IDisposable
                {
                    public void Dispose() => throw null;
                    public ValueTask DisposeAsync() => throw null;
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Using_Diagnostic1_WithComment()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    // MA0042 "Prefer using 'await using'"
                    {|MA0042:using var a = new Sample();|}
                }

                private class Sample : IDisposable
                {
                    public void Dispose() => throw null;
                    public ValueTask DisposeAsync() => throw null;
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Using_Diagnostic2()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            using System.Diagnostics;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using (var b = new Sample()) { }|}
                }

                private class Sample : IDisposable
                {
                    public void Dispose() => throw null;
                    public ValueTask DisposeAsync() => throw null;
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Using_Diagnostic3()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            using System.Diagnostics;

            class Test
            {
                public async Task A()
                {
                    var sample = new Sample();
                    {|MA0042:using (sample) { }|}
                }

                private class Sample : IDisposable
                {
                    public void Dispose() => throw null;
                    public ValueTask DisposeAsync() => throw null;
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Using_Diagnostic4()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExtensionMethod()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A()|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericArgument_MultipleIncompatibleGenericArguments_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExtensionMethod_GenericArgumentsIncompatible_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericArgument_ListToIEnumerable_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericArgument_NestedGenericIncompatibility_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericArgument_SameOriginalDefinitionButDifferentTypeParameterMapping_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericArgument_SingleGenericArgument_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A<int>(new List<int>())|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExtensionMethod_GenericArgument_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A<int>(new List<int>())|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericArgument_ArrayOfGenericArgument_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A<int>(new int[1])|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericArgument_AsyncConstraintIncompatible_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_InModifierDifference_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A(in value)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_RefMismatch_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_OutMismatch_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_NullLiteral_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A(null)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_DefaultLiteral_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A(default)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_ImplicitNumericConversion_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A(42)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_ImplicitNumericWidening_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A(1)|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_ImplicitNumericNarrowing_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_ImplicitNumericToFloatingPoint_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A(1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_ImplicitNumericFloatingPointToInteger_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_ImplicitNumericInt64ToFloatingPoint_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_ImplicitNumericInt32ToFloatingPoint_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_ImplicitNumericByteToInt32_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A((byte)1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_ImplicitNumericInt16ToInt32_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A((short)1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_ImplicitNumericSingleToDouble_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A(1f)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_ImplicitNumericHalfToSingle_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A((Half)1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_ImplicitNumericHalfToDouble_ShouldReport()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
        test.TestCode = """
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
                    {|MA0042:new Test().A((Half)1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericArgument_CompatibleGenericDefinitions_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A(new List<int>())|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericArgument_DifferentArity_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericArgument_ConstraintNewIncompatible_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericArgument_DifferentTypeConstraintsOrder_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericMethod_SameConstraints_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A<Mark>(1, new List<Mark>())|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_ImplicitUserDefinedConversion_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A("value")|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExtensionMethodToInstanceMethod_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A(1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Argument_NamedArgumentsReordered_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:new Test().A(right: 2, left: 1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CreateAsyncScope()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddAspNetCore();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;

            class demo
            {
                public async Task a()
                {
                    IServiceProvider provider = null;
                    await using var scope1 = provider.CreateAsyncScope();
                    using var scope2 = {|MA0042:provider.CreateScope()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CreateAsyncScope_net5()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net50.AddAspNetCore("5.0.0");
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DbContext_Add()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddEntityFrameworkCore();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/891")]
    public Task IDbContextFactory_CreateDbContext_NoReport()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddEntityFrameworkCore();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task SqliteConnection_CreateCommand_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddSqlite();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.Data.Sqlite;

            class Test
            {
                public async Task A(SqliteConnection connection)
                {
                    using var command = connection.CreateCommand();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task SqliteConnection_Close_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddSqlite();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.Data.Sqlite;

            class Test
            {
                public async Task A(SqliteConnection connection)
                {
                    connection.Close();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task SqliteCommand_Prepare_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddSqlite();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.Data.Sqlite;

            class Test
            {
                public async Task A(SqliteCommand command)
                {
                    command.Prepare();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task SqliteConnection_CreateCommand_OptionDisabled_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0042.enable_sqlite_special_cases", "false");
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddSqlite();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.Data.Sqlite;

            class Test
            {
                public async Task A(SqliteConnection connection)
                {
                    {|MA0042:using var command = connection.CreateCommand();|}
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task SqliteConnection_Close_OptionDisabled_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0042.enable_sqlite_special_cases", "false");
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddSqlite();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.Data.Sqlite;

            class Test
            {
                public async Task A(SqliteConnection connection)
                {
                    {|MA0042:connection.Close()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task SqliteCommand_Prepare_OptionDisabled_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0042.enable_sqlite_special_cases", "false");
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddSqlite();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.Data.Sqlite;

            class Test
            {
                public async Task A(SqliteCommand command)
                {
                    {|MA0042:command.Prepare()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task SqliteDataReader_Read_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddSqlite();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.Data.Sqlite;

            class Test
            {
                public async Task A(SqliteDataReader reader)
                {
                    reader.Read();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1121")]
    public Task SqliteDataReader_Read_OptionDisabled_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0042.enable_sqlite_special_cases", "false");
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddSqlite();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.Data.Sqlite;

            class Test
            {
                public async Task A(SqliteDataReader reader)
                {
                    {|MA0042:reader.Read()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1333")]
    public Task DbConnectionAssignedFromSqliteConnection_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddSqlite();
        test.TestCode = """
            using System.Data.Common;
            using System.Threading.Tasks;
            using Microsoft.Data.Sqlite;

            class Test
            {
                public async Task A()
                {
                    DbConnection connection = new SqliteConnection();
                    connection.Close();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1333")]
    public Task DbConnectionNotAssignedFromSqliteConnection_Diagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddSqlite();
        test.TestCode = """
            using System.Data.Common;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A(DbConnection connection)
                {
                    {|MA0042:connection.Close()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IAsyncEnumerable()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            class demo
            {
                public IAsyncEnumerable<int> A()
                {
                    {|MA0042:Thread.Sleep(1)|};
                    throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IAsyncEnumerator()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            class demo
            {
                public IAsyncEnumerator<int> A()
                {
                    {|MA0042:Thread.Sleep(1)|};
                    throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AsyncMethodBuilder()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            [System.Runtime.CompilerServices.AsyncMethodBuilderAttribute(typeof(int))]
            class Sample
            {
                public Sample A()
                {
                    {|MA0042:Thread.Sleep(1)|};
                    throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TaskYieldResult()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TopLevelStatement()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.WindowsApplication;
        test.TestCode = """
            {|MA0042:System.Threading.Thread.Sleep(1)|};
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TaskRunDelegate()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            class Sample
            {
                public void A()
                {
                    _ = Task.Run(() => {|MA0042:Thread.Sleep(1)|});
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Moq_Raise()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddMoq();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewMemoryStream()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using var ms = new System.IO.MemoryStream();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingFactoryMethod_StreamSubclass_NoDisposeAsyncOverride_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var s = CreateStream();|}
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewStreamSubclass_WithDisposeAsyncOverride_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var s = new MyStream();|}
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SemaphoreSlim_Wait_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SemaphoreSlim_Wait_FlowedZero_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading;
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    var semaphore = new SemaphoreSlim(1);
                    var timeout = 0;
                    semaphore.Wait(timeout);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SemaphoreSlim_Wait_TimeSpanZero_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SemaphoreSlim_Wait_NonZero_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading;
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    var semaphore = new SemaphoreSlim(1);
                    {|MA0042:semaphore.Wait(100)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SemaphoreSlim_Wait_NoArgs_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading;
            using System.Threading.Tasks;
            class Test
            {
                public async Task A()
                {
                    var semaphore = new SemaphoreSlim(1);
                    {|MA0042:semaphore.Wait()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SemaphoreSlim_Wait_ZeroWithCancellationToken_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TemporaryDirectory_InTestProject_WithXunit_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddXunitV3();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TemporaryDirectory_InTestProject_WithNUnit_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddNUnit();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TemporaryDirectory_InTestProject_WithMSTest_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddMSTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TemporaryDirectory_InNonTestProject_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
                    {|MA0042:dir.CreateTextFile("test.txt", "content")|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewDbConnectionSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingFactoryMethod_DbConnectionSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingFactoryMethod_DbConnectionSubclass_NoDisposeAsyncOverride_OptionDisabled_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0042.enable_db_special_cases", "false");
        test.TestCode = """
            using System.Data;
            using System.Data.Common;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var conn = CreateConnection();|}
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewDbConnectionSubclass_WithDisposeAsyncOverride_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Data;
            using System.Data.Common;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var conn = new MySqlConnection();|}
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewDbConnectionSubclass_DisposeAsyncOverriddenInIntermediateBase_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Data;
            using System.Data.Common;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var conn = new DerivedConnection();|}
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewDbCommandSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingFactoryMethod_DbCommandSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingFactoryMethod_DbCommandSubclass_NoDisposeAsyncOverride_OptionDisabled_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0042.enable_db_special_cases", "false");
        test.TestCode = """
            using System.Data;
            using System.Data.Common;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var command = CreateCommand();|}
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewDbCommandSubclass_WithDisposeAsyncOverride_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Data;
            using System.Data.Common;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var command = new MyDbCommand();|}
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewDbCommandSubclass_DisposeAsyncOverriddenInIntermediateBase_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Data;
            using System.Data.Common;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var command = new DerivedDbCommand();|}
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewDbDataReaderSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Data;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    using var reader = new DataTableReader(new DataTable());
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingFactoryMethod_DbDataReaderSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingFactoryMethod_DbDataReaderSubclass_NoDisposeAsyncOverride_OptionDisabled_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0042.enable_db_special_cases", "false");
        test.TestCode = """
            using System.Data;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var reader = CreateReader();|}
                }

                private DataTableReader CreateReader() => new DataTableReader(new DataTable());
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewDbDataReaderSubclass_WithDisposeAsyncOverride_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Collections;
            using System.Data.Common;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var reader1 = new MyDbDataReader();|}
                    {|MA0042:using var reader2 = new DerivedDbDataReader();|}
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewDbTransactionSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingFactoryMethod_DbTransactionSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingFactoryMethod_DbTransactionSubclass_NoDisposeAsyncOverride_OptionDisabled_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0042.enable_db_special_cases", "false");
        test.TestCode = """
            using System.Data;
            using System.Data.Common;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var transaction = CreateTransaction();|}
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewDbTransactionSubclass_WithDisposeAsyncOverride_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Data;
            using System.Data.Common;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var transaction1 = new MyDbTransaction();|}
                    {|MA0042:using var transaction2 = new DerivedDbTransaction();|}
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewDbBatchSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingFactoryMethod_DbBatchSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingFactoryMethod_DbBatchSubclass_NoDisposeAsyncOverride_OptionDisabled_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0042.enable_db_special_cases", "false");
        test.TestCode = """
            using System.Data;
            using System.Data.Common;
            using System.Threading;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var batch = CreateBatch();|}
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewDbBatchSubclass_WithDisposeAsyncOverride_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Data;
            using System.Data.Common;
            using System.Threading;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var batch1 = new MyDbBatch();|}
                    {|MA0042:using var batch2 = new DerivedDbBatch();|}
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewTextWriterSubclass_NoDisposeAsyncOverride_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingFactoryMethod_TextWriterSubclass_NoDisposeAsyncOverride_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            using System.Text;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var writer = CreateTextWriter();|}
                }

                private MyTextWriter CreateTextWriter() => new MyTextWriter();
            }

            class MyTextWriter : TextWriter
            {
                public override Encoding Encoding => Encoding.UTF8;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingNewTextWriterSubclass_WithDisposeAsyncOverride_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            using System.Text;
            using System.Threading.Tasks;

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var writer1 = new MyTextWriter();|}
                    {|MA0042:using var writer2 = new DerivedTextWriter();|}
                }
            }

            class MyTextWriter : TextWriter
            {
                public override Encoding Encoding => Encoding.UTF8;
                public override ValueTask DisposeAsync() => throw null;
            }

            class DerivedTextWriter : MyTextWriter { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludeFromBlockingCallAnalysisAttribute_DocumentationIdMethod()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute("M:System.Threading.Tasks.Task.Wait")]

            class Test
            {
                public async Task A()
                {
                    Task.Delay(1).Wait();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludeFromBlockingCallAnalysisAttribute_DocumentationIdProperty()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute("P:System.Threading.Tasks.Task`1.Result")]

            class Test
            {
                public async Task A()
                {
                    _ = Task.FromResult(1).Result;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludeFromBlockingCallAnalysisAttribute_DoesNotAffectAwaitUsing()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute("M:System.Threading.Tasks.Task.Wait")]

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var value = new AsyncDisposable();|}
                }
            }

            class AsyncDisposable : IDisposable, IAsyncDisposable
            {
                public void Dispose() { }
                public ValueTask DisposeAsync() => default;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExcludeFromBlockingCallAnalysisAttribute_MethodSignature_DoesNotAffectAwaitUsing()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute(typeof(Test), "Create")]

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var value = Create();|}
                }

                private AsyncDisposable Create() => throw null;
            }

            class AsyncDisposable : IDisposable, IAsyncDisposable
            {
                public void Dispose() { }
                public ValueTask DisposeAsync() => default;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAwaitableTypeAttribute_DoesNotAffectAwaitUsing()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(AsyncDisposable))]

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var value = new AsyncDisposable();|}
                }
            }

            class AsyncDisposable : IDisposable, IAsyncDisposable
            {
                public void Dispose() { }
                public ValueTask DisposeAsync() => default;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAsyncDisposableTypeAttribute_DoesAffectAwaitUsing()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAsyncDisposableTypeAttribute(typeof(AsyncDisposable))]

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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAwaitableTypeAttribute_DoesNotAffectTaskWrappedAwaitSuggestion()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(AwaitResult))]

            class Test
            {
                public async Task A()
                {
                    {|MA0042:Create()|};
                }

                private AwaitResult Create() => throw null;
                private Task<AwaitResult> CreateAsync() => throw null;
            }

            class AwaitResult { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAwaitableTypeAttribute_OpenGenericType_DoesNotAffectTaskWrappedAwaitSuggestion()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(AwaitResult<>))]

            class Test
            {
                public async Task A()
                {
                    {|MA0042:Create()|};
                }

                private AwaitResult<int> Create() => throw null;
                private Task<AwaitResult<int>> CreateAsync() => throw null;
            }

            class AwaitResult<T> { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAwaitableTypeAttribute_DoesNotAffectOtherTypes()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(OtherResult))]

            class Test
            {
                public async Task A()
                {
                    {|MA0042:Create()|};
                }

                private AwaitResult Create() => throw null;
                private Task<AwaitResult> CreateAsync() => throw null;
            }

            class AwaitResult { }
            class OtherResult { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAwaitableTypeAttribute_DoesNotAffectDerivedType_AwaitUsing()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(BaseAsyncDisposable))]

            class Test
            {
                public async Task A()
                {
                    {|MA0042:using var value = new DerivedAsyncDisposable();|}
                }
            }

            class BaseAsyncDisposable : IDisposable, IAsyncDisposable
            {
                public void Dispose() { }
                public ValueTask DisposeAsync() => default;
            }

            class DerivedAsyncDisposable : BaseAsyncDisposable { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAwaitableTypeAttribute_DoesNotAffectDerivedType_AwaitSuggestion()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(BaseResult))]

            class Test
            {
                public async Task A()
                {
                    {|MA0042:Create()|};
                }

                private BaseResult Create() => throw null;
                private Task<DerivedResult> CreateAsync() => throw null;
            }

            class BaseResult { }
            class DerivedResult : BaseResult { }
            """;

        return test.RunAsync();
    }
}

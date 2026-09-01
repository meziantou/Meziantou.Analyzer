using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseConfigureAwaitAnalyzer,
    Meziantou.Analyzer.Rules.UseConfigureAwaitFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseConfigureAwaitAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard21;
        return test;
    }

    [Fact]
    public Task MissingConfigureAwait_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    {|MA0004:await Task.Delay(1)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await Task.Delay(1).ConfigureAwait(false);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingConfigureAwait_AwaitForeach_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    IAsyncEnumerable<int> Enumerable() => throw null;

                    await foreach(var item in {|MA0004:Enumerable()|})
                    {
                    }
                }
            }
            """;
        test.FixedCode = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    IAsyncEnumerable<int> Enumerable() => throw null;

                    await foreach(var item in Enumerable().ConfigureAwait(false))
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingConfigureAwait_AwaitForeach_ShouldReportError_ConfigureAwait()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using System.Runtime.CompilerServices;
            class ClassTest
            {
                async Task Test()
                {
                    Task<IAsyncEnumerable<int>> Enumerable() => throw null;

                    await foreach(var item in {|MA0004:await Enumerable().ConfigureAwait(false)|})
                    {
                    }
                }
            }
            """;
        test.FixedCode = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using System.Runtime.CompilerServices;
            class ClassTest
            {
                async Task Test()
                {
                    Task<IAsyncEnumerable<int>> Enumerable() => throw null;

                    await foreach(var item in (await Enumerable().ConfigureAwait(false)).ConfigureAwait(false))
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingConfigureAwait_AwaitForeach_WithCancellation_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    IAsyncEnumerable<int> Enumerable() => throw null;

                    CancellationToken ct = default;
                    await foreach(var item in {|MA0004:Enumerable().WithCancellation(ct)|})
                    {
                    }
                }
            }
            """;
        test.FixedCode = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    IAsyncEnumerable<int> Enumerable() => throw null;

                    CancellationToken ct = default;
                    await foreach(var item in Enumerable().WithCancellation(ct).ConfigureAwait(false))
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingConfigureAwait_AwaitForeach_WithConfigureAwait()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    IAsyncEnumerable<int> Enumerable() => throw null;

                    CancellationToken ct = default;
                    await foreach(var item in Enumerable().ConfigureAwait(false))
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingConfigureAwait_AwaitDispose_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await using var {|MA0004:a = new AsyncDisposable()|};
                    Console.WriteLine();
                }
            }
            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;
        test.FixedCode = """
            using System;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    var a = new AsyncDisposable();
                    await using (a.ConfigureAwait(false))
                    {
                        Console.WriteLine();
                    }
                }
            }
            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingConfigureAwait_AwaitDispose_Block_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await using (var {|MA0004:a = new AsyncDisposable()|})
                    {
                    }
                }
            }
            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;
        test.FixedCode = """
            using System;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    var a = new AsyncDisposable();
                    await using (a.ConfigureAwait(false))
                    {
                    }
                }
            }
            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingConfigureAwait_AwaitDispose_TopLevelStatement_ShouldReportError()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System;
            using System.Threading.Tasks;

            await using var {|MA0004:a = new AsyncDisposable()|};
            Console.WriteLine();

            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;
        test.FixedCode = """
            using System;
            using System.Threading.Tasks;

            var a = new AsyncDisposable();

            await using (a.ConfigureAwait(false))
            {
                Console.WriteLine();
            }

            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingConfigureAwait_AwaitDispose_Block_TopLevelStatement_ShouldReportError()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            await using (var {|MA0004:a = new AsyncDisposable()|})
            {
            }

            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;
        test.FixedCode = """
            using System;
            using System.Threading.Tasks;
            var a = new AsyncDisposable();

            await using (a.ConfigureAwait(false))
            {
            }

            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingConfigureAwait_AwaitDispose_BlockWithoutVariable()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await using ({|MA0004:new AsyncDisposable()|})
                    {
                    }
                }
            }
            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;
        test.FixedCode = """
            using System;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await using (new AsyncDisposable().ConfigureAwait(false))
                    {
                    }
                }
            }
            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConfigureAwaitIsPresent_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await Task.Delay(1).ConfigureAwait(true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConfigureAwaitOfTIsPresent_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await Task.Run(() => 10).ConfigureAwait(true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingConfigureAwaitInWpfWindowClass_ShouldNotReportError()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Wpf;
        test.TestCode = """
            using System.Threading.Tasks;
            class MyClass : System.Windows.Window
            {
                async Task Test()
                {
                    await Task.Delay(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingConfigureAwaitInWpfCommandClass_ShouldNotReportError()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Wpf;
        test.TestCode = """
            using System.Threading.Tasks;
            class MyClass : System.Windows.Input.ICommand
            {
                public void Execute(object o) => throw null;
                public bool CanExecute(object o) => throw null;
                public event System.EventHandler CanExecuteChanged;

                async Task Test()
                {
                    await Task.Delay(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AfterConfigureAwaitFalse_AllAwaitShouldUseConfigureAwait()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Wpf;
        test.TestCode = """
            using System.Threading.Tasks;
            class MyClass : System.Windows.Window
            {
                async Task Test()
                {
                    await Task.Delay(1);
                    await Task.Delay(1).ConfigureAwait(false);
                    {|MA0004:await Task.Delay(1)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class MyClass : System.Windows.Window
            {
                async Task Test()
                {
                    await Task.Delay(1);
                    await Task.Delay(1).ConfigureAwait(false);
                    await Task.Delay(1).ConfigureAwait(false);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AfterConfigureAwaitFalseInANonAccessibleBranch_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Wpf;
        test.TestCode = """
            using System.Threading.Tasks;
            class MyClass : System.Windows.Window
            {
                async Task Test()
                {
                    bool a = true;
                    if (a)
                    {
                        await Task.Delay(1).ConfigureAwait(false);
                        return;
                    }

                    await Task.Delay(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AfterConfigureAwaitFalseInNonAccessibleBranch2_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Wpf;
        test.TestCode = """
            using System.Threading.Tasks;
            class MyClass : System.Windows.Window
            {
                async Task Test()
                {
                    bool a = true;
                    if (a)
                    {
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    else
                    {
                        {|MA0004:await Task.Delay(1)|};
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TaskYield_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await Task.Yield();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task XUnitAttribute_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddXunitV3();
        test.TestCode = """
            using System.Threading.Tasks;
            class ClassTest
            {
                [Xunit.Fact]
                async Task Test()
                {
                    await Task.Delay(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Blazor_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            namespace Microsoft.AspNetCore.Components
            {
                public interface IComponent
                {
                }
            }

            class ClassTest : Microsoft.AspNetCore.Components.IComponent
            {
                async Task Test()
                {
                    await Task.Delay(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Blazor_ConfigurationAlways_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0004.report", "always");
        test.TestCode = """
            using System.Threading.Tasks;
            namespace Microsoft.AspNetCore.Components
            {
                public interface IComponent
                {
                }
            }

            class ClassTest : Microsoft.AspNetCore.Components.IComponent
            {
                async Task Test()
                {
                    {|MA0004:await Task.Delay(1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitForEach_VariableAlreadyAwaited()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    IAsyncEnumerable<int> Enumerable() => throw null;

                    var temp = Enumerable().ConfigureAwait(false);
                    await foreach(var item in temp)
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitUsing_ConfiguredNextStatement()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net60.AddAspNetCore("6.0.10");
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            class ClassTest
            {
                async Task Test()
                {
                    ServiceProvider services = null!;
                    AsyncServiceScope scope = services.CreateAsyncScope();
                    await using (scope.ConfigureAwait(false))
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitUsingAwait()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await using var {|MA0004:a = await CreateDisposableAsync().ConfigureAwait(false)|};
                }

                async Task<IAsyncDisposable> CreateDisposableAsync() => throw null;
            }
            """;
        test.FixedCode = """
            using System;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    var a = await CreateDisposableAsync().ConfigureAwait(false);
                    await using (a.ConfigureAwait(false))
                    {
                    }
                }

                async Task<IAsyncDisposable> CreateDisposableAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitUsing_WithMultipleUsings_ShouldNotThrow()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.IO;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    Stream stream = OpenWrite();
                    await using (stream.ConfigureAwait(false))
                    await using (var {|MA0004:streamWriter = new StreamWriter(stream)|})
                    {
                        await streamWriter.WriteAsync("test-data").ConfigureAwait(false);
                    }
                }

                Stream OpenWrite() => throw null;
            }
            """;
        test.FixedCode = """
            using System;
            using System.IO;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    Stream stream = OpenWrite();
                    var streamWriter = new StreamWriter(stream);
                    await using (stream.ConfigureAwait(false))
                    await using (streamWriter.ConfigureAwait(false))
                    {
                        await streamWriter.WriteAsync("test-data").ConfigureAwait(false);
                    }
                }

                Stream OpenWrite() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitUsingVar_InsideConfiguredUsing_ShouldNotThrow()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    var connection = new AsyncDisposable();
                    await using (connection.ConfigureAwait(false))
                    {
                        await using var {|MA0004:command = new AsyncDisposable()|};
                    }
                }
            }
            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;
        test.FixedCode = """
            using System;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    var connection = new AsyncDisposable();
                    await using (connection.ConfigureAwait(false))
                    {
                        var command = new AsyncDisposable();
                        await using (command.ConfigureAwait(false))
                        {
                        }
                    }
                }
            }
            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitUsingAwait_NoVariable()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            await using ({|MA0004:await A().ConfigureAwait(false)|})
            {
            }

            Task<IAsyncDisposable> A() => throw null;
            """;
        test.FixedCode = """
            using System;
            using System.Threading.Tasks;
            await using ((await A().ConfigureAwait(false)).ConfigureAwait(false))
            {
            }

            Task<IAsyncDisposable> A() => throw null;
            """;

        return test.RunAsync();
    }
}

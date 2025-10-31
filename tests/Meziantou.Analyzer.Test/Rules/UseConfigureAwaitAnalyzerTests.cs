using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using Microsoft.CodeAnalysis;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseConfigureAwaitAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.NetStandard2_1)
            .WithAnalyzer<UseConfigureAwaitAnalyzer>()
            .WithCodeFixProvider<UseConfigureAwaitFixer>();
    }

    [Fact]
    public async Task MissingConfigureAwait_ShouldReportError()
    {
        const string SourceCode = """
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    [||]await Task.Delay(1);
                }
            }
            """;
        const string CodeFix = """
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await Task.Delay(1).ConfigureAwait(false);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task MissingConfigureAwait_AwaitForeach_ShouldReportError()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    IAsyncEnumerable<int> Enumerable() => throw null;
            
                    await foreach(var item in [||]Enumerable())
                    {
                    }
                }
            }
            """;
        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task MissingConfigureAwait_AwaitForeach_ShouldReportError_ConfigureAwait()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using System.Runtime.CompilerServices;
            class ClassTest
            {
                async Task Test()
                {
                    Task<IAsyncEnumerable<int>> Enumerable() => throw null;
            
                    await foreach(var item in [||]await Enumerable().ConfigureAwait(false))
                    {
                    }
                }
            }
            """;
        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task MissingConfigureAwait_AwaitForeach_WithCancellation_ShouldReportError()
    {
        const string SourceCode = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    IAsyncEnumerable<int> Enumerable() => throw null;
            
                    CancellationToken ct = default;
                    await foreach(var item in [||]Enumerable().WithCancellation(ct))
                    {
                    }
                }
            }
            """;
        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task MissingConfigureAwait_AwaitForeach_WithConfigureAwait()
    {
        const string SourceCode = """
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

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MissingConfigureAwait_AwaitDispose_ShouldReportError()
    {
        const string SourceCode = """
            using System;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await using var [||]a = new AsyncDisposable();
                    Console.WriteLine();
                }
            }
            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;

        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task MissingConfigureAwait_AwaitDispose_Block_ShouldReportError()
    {
        const string SourceCode = """
            using System;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await using (var [||]a = new AsyncDisposable())
                    {
                    }
                }
            }
            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;

        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task MissingConfigureAwait_AwaitDispose_TopLevelStatement_ShouldReportError()
    {
        const string SourceCode = """
            using System;
            using System.Threading.Tasks;

            await using var [||]a = new AsyncDisposable();
            Console.WriteLine();

            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;

        const string CodeFix = """
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

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .WithOutputKind(OutputKind.ConsoleApplication)
              .ValidateAsync();
    }

    [Fact]
    public async Task MissingConfigureAwait_AwaitDispose_Block_TopLevelStatement_ShouldReportError()
    {
        const string SourceCode = """
            using System;
            using System.Threading.Tasks;
            await using (var [||]a = new AsyncDisposable())
            {
            }

            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;

        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .WithOutputKind(@OutputKind.ConsoleApplication)
              .ValidateAsync();
    }

    [Fact]
    public async Task MissingConfigureAwait_AwaitDispose_BlockWithoutVariable()
    {
        const string SourceCode = """
            using System;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await using ([||]new AsyncDisposable())
                    {
                    }
                }
            }
            class AsyncDisposable : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => throw null;
            }
            """;

        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ConfigureAwaitIsPresent_ShouldNotReportError()
    {
        const string SourceCode = """
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await Task.Delay(1).ConfigureAwait(true);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ConfigureAwaitOfTIsPresent_ShouldNotReportError()
    {
        const string SourceCode = """
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await Task.Run(() => 10).ConfigureAwait(true);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MissingConfigureAwaitInWpfWindowClass_ShouldNotReportError()
    {
        const string SourceCode = """
            using System.Threading.Tasks;
            class MyClass : System.Windows.Window
            {
                async Task Test()
                {
                    await Task.Delay(1);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net4_8)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MissingConfigureAwaitInWpfCommandClass_ShouldNotReportError()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net4_8)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AfterConfigureAwaitFalse_AllAwaitShouldUseConfigureAwait()
    {
        const string SourceCode = """
            using System.Threading.Tasks;
            class MyClass : System.Windows.Window
            {
                async Task Test()
                {
                    await Task.Delay(1);
                    await Task.Delay(1).ConfigureAwait(false);
                    [||]await Task.Delay(1);
                }
            }
            """;
        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net4_8)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task AfterConfigureAwaitFalseInANonAccessibleBranch_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net4_8)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AfterConfigureAwaitFalseInNonAccessibleBranch2_ShouldReportDiagnostic()
    {
        const string SourceCode = """
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
                        [||]await Task.Delay(1);
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net4_8)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task TaskYield_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await Task.Yield();
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task XUnitAttribute_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .AddXUnitApi()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Blazor_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Blazor_ConfigurationAlways_ShouldReportDiagnostic()
    {
        const string SourceCode = """
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
                    [||]await Task.Delay(1);
                }
            }
            """;
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration("MA0004.report", "always")
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AwaitForEach_VariableAlreadyAwaited()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AwaitUsing_ConfiguredNextStatement()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.AspNetCore6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AwaitUsingAwait()
    {
        const string SourceCode = """
            using System;
            using System.Threading.Tasks;
            class ClassTest
            {
                async Task Test()
                {
                    await using var [||]a = await CreateDisposableAsync().ConfigureAwait(false);
                }
            
                async Task<IAsyncDisposable> CreateDisposableAsync() => throw null;
            }
            """;
        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task AwaitUsingAwait_NoVariable()
    {
        const string SourceCode = """
            using System;
            using System.Threading.Tasks;
            await using ([||]await A().ConfigureAwait(false))
            {
            }

            Task<IAsyncDisposable> A() => throw null;
            """;

        const string CodeFix = """
            using System;
            using System.Threading.Tasks;
            await using ((await A().ConfigureAwait(false)).ConfigureAwait(false))
            {
            }
            
            Task<IAsyncDisposable> A() => throw null;
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .WithOutputKind(@OutputKind.ConsoleApplication)
              .ValidateAsync();
    }
}

using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ReturnTaskInsteadOfAwaitingItAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<ReturnTaskInsteadOfAwaitingItAnalyzer>()
            .WithCodeFixProvider<ReturnTaskInsteadOfAwaitingItFixer>()
            .WithTargetFramework(TargetFramework.Net8_0);
    }

    [Fact]
    public void Rule_IsDisabledByDefault()
    {
        var rule = new ReturnTaskInsteadOfAwaitingItAnalyzer().SupportedDiagnostics[0];
        Assert.False(rule.IsEnabledByDefault);
    }

    [Fact]
    public async Task ReturnAwait_TaskOfT()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> A()
                    {
                        return [|await Inner()|];
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    Task<int> A()
                    {
                        return Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExpressionBody_TaskOfT()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> A() => [|await Inner()|];
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    Task<int> A() => Inner();
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Await_NonGenericTask_AddsReturn()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task Inner() => throw null;
                    async Task A()
                    {
                        [|await Inner()|];
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task Inner() => throw null;
                    Task A()
                    {
                        return Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExpressionBody_NonGenericTask()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task Inner() => throw null;
                    async Task A() => [|await Inner()|];
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task Inner() => throw null;
                    Task A() => Inner();
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ConfigureAwait_IsStripped()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task Inner() => throw null;
                    async Task A()
                    {
                        [|await Inner().ConfigureAwait(false)|];
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task Inner() => throw null;
                    Task A()
                    {
                        return Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task LocalFunction()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    void A()
                    {
                        async Task<int> Local() => [|await Inner()|];
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    void A()
                    {
                        Task<int> Local() => Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Lambda()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    void A()
                    {
                        Func<Task<int>> f = async () => [|await Inner()|];
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System;
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    void A()
                    {
                        Func<Task<int>> f = () => Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExtraStatementBefore_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> A()
                    {
                        System.Console.WriteLine();
                        return await Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task InsideTry_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> A()
                    {
                        try
                        {
                            return await Inner();
                        }
                        catch
                        {
                            return 0;
                        }
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task InsideUsing_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> A(IDisposable d)
                    {
                        using (d)
                        {
                            return await Inner();
                        }
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task MultipleAwaits_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> A()
                    {
                        return await Inner() + await Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NotAssignable_ValueTaskAwaitingTask_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task Inner() => throw null;
                    async ValueTask A()
                    {
                        await Inner();
                    }
                }
                """)
            .ValidateAsync();
    }
}

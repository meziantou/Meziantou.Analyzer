using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseAwaitInsteadOfReturningTaskAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseAwaitInsteadOfReturningTaskAnalyzer>()
            .WithCodeFixProvider<UseAwaitInsteadOfReturningTaskFixer>()
            .WithTargetFramework(TargetFramework.Net8_0);
    }

    [Fact]
    public void Rule_IsDisabledByDefault()
    {
        var rule = new UseAwaitInsteadOfReturningTaskAnalyzer().SupportedDiagnostics[0];
        Assert.False(rule.IsEnabledByDefault);
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
                    Task<int> A() => [|Inner()|];
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> A() => await Inner();
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task BlockBody_TaskOfT()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    Task<int> A()
                    {
                        return [|Inner()|];
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> A()
                    {
                        return await Inner();
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
                    Task A() => [|Inner()|];
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task Inner() => throw null;
                    async Task A() => await Inner();
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task BlockBody_NonGenericTask_DropsReturn()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task Inner() => throw null;
                    Task A()
                    {
                        return [|Inner()|];
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task Inner() => throw null;
                    async Task A()
                    {
                        await Inner();
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
                        Task<int> Local() => [|Inner()|];
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
                        async Task<int> Local() => await Inner();
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
                        Func<Task<int>> f = () => [|Inner()|];
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
                        Func<Task<int>> f = async () => await Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExpressionBody_ValueTaskOfT()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    ValueTask<int> Inner() => throw null;
                    ValueTask<int> A() => [|Inner()|];
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    ValueTask<int> Inner() => throw null;
                    async ValueTask<int> A() => await Inner();
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExpressionBody_NonGenericValueTask()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    ValueTask Inner() => throw null;
                    ValueTask A() => [|Inner()|];
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    ValueTask Inner() => throw null;
                    async ValueTask A() => await Inner();
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task MethodWithAccessModifier()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    public Task<int> A() => [|Inner()|];
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    public async Task<int> A() => await Inner();
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StaticLocalFunction()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    static Task<int> Inner() => throw null;
                    void A()
                    {
                        static Task<int> Local() => [|Inner()|];
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    static Task<int> Inner() => throw null;
                    void A()
                    {
                        static async Task<int> Local() => await Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task SimpleLambda()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner(int x) => throw null;
                    void A()
                    {
                        Func<int, Task<int>> f = x => [|Inner(x)|];
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System;
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner(int x) => throw null;
                    void A()
                    {
                        Func<int, Task<int>> f = async x => await Inner(x);
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task AnonymousMethod()
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
                        Func<Task<int>> f = delegate { return [|Inner()|]; };
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
                        Func<Task<int>> f = async delegate { return await Inner(); };
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task PropertyGetter_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    Task<int> P
                    {
                        get { return Inner(); }
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Operator_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    static Task<int> Inner() => throw null;
                    public static Task<int> operator +(Test a, Test b) => Inner();
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task AlreadyAsync_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> A() => await Inner();
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ReturnNull_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task A() => null;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task MultipleStatements_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    Task<int> A()
                    {
                        System.Console.WriteLine();
                        return Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NonAwaitableReturn_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Test
                {
                    int A() => 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task AsyncEnumerable_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Collections.Generic;
                class Test
                {
                    IAsyncEnumerable<int> Inner() => throw null;
                    IAsyncEnumerable<int> A() => Inner();
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExpressionTree_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;
                using System.Linq.Expressions;
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    void A()
                    {
                        Expression<Func<Task<int>>> f = () => Inner();
                    }
                }
                """)
            .ValidateAsync();
    }
}

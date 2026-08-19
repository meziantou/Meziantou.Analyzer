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
    public async Task Issue1280_StaticVoidTaskWithConfigureAwaitAndLambdaArgument()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                class Test
                {
                    static Task InvokeDialogActionAsync(Action action) => throw null;

                    internal static async Task CloseNotifyIconAsync()
                    {
                        [|await InvokeDialogActionAsync(() => { }).ConfigureAwait(false)|];
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System;
                using System.Threading.Tasks;
                class Test
                {
                    static Task InvokeDialogActionAsync(Action action) => throw null;

                    internal static Task CloseNotifyIconAsync()
                    {
                        return InvokeDialogActionAsync(() => { });
                    }
                }
                """)
            .ValidateAsync();
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
    public async Task ExpressionBody_ValueTaskOfT()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    ValueTask<int> Inner() => throw null;
                    async ValueTask<int> A() => [|await Inner()|];
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    ValueTask<int> Inner() => throw null;
                    ValueTask<int> A() => Inner();
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ConfigureAwaitTrue_Generic_IsStripped()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> A()
                    {
                        return [|await Inner().ConfigureAwait(true)|];
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
                        static async Task<int> Local() => [|await Inner()|];
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
                        static Task<int> Local() => Inner();
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
                        Func<int, Task<int>> f = async x => [|await Inner(x)|];
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
                        Func<int, Task<int>> f = x => Inner(x);
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
                        Func<Task<int>> f = async delegate { return [|await Inner()|]; };
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
                        Func<Task<int>> f = delegate { return Inner(); };
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NestedAwaitInOperand_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    Task<int> Outer(int value) => throw null;
                    async Task<int> A()
                    {
                        return await Outer(await Inner());
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task CovariantReturnTypeMismatch_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<string> Inner() => throw null;
                    async Task<object> A()
                    {
                        return await Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task CustomAwaitableNotAssignableToTask_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    async Task A()
                    {
                        await Task.Yield();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task AwaitAssignedToDiscard_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task A()
                    {
                        _ = await Inner();
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
    public async Task MultipleMethods_BatchFix()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    Task Inner2() => throw null;
                    async Task<int> A() => [|await Inner()|];
                    async Task B() => [|await Inner2()|];
                    async Task<int> C()
                    {
                        return [|await Inner()|];
                    }
                    async Task D()
                    {
                        [|await Inner2()|];
                    }
                }
                """)
            .ShouldBatchFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    Task Inner2() => throw null;
                    Task<int> A() => Inner();
                    Task B() => Inner2();
                    Task<int> C()
                    {
                        return Inner();
                    }
                    Task D()
                    {
                        return Inner2();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task PrecedingStatement()
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
                        System.Console.WriteLine();
                        return Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task MultipleReturns_TaskOfT()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> A(bool condition)
                    {
                        if (condition)
                            return [|await Inner()|];

                        return [|await Inner()|];
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    Task<int> A(bool condition)
                    {
                        if (condition)
                            return Inner();

                        return Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NestedLocalFunctionAwait_DoesNotAffectParent()
    {
        // The local function has a non-removable await (Task.Yield) but must not prevent the parent from being reported
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> Parent()
                    {
                        async Task<int> Local()
                        {
                            await Task.Yield();
                            return await Inner();
                        }

                        return [|await Local()|];
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    Task<int> Parent()
                    {
                        async Task<int> Local()
                        {
                            await Task.Yield();
                            return await Inner();
                        }

                        return Local();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task LocalFunctionReported_ParentIsNot()
    {
        // The parent has a non-removable await (Task.Yield); only the local function is reported
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> Parent()
                    {
                        async Task<int> Local() => [|await Inner()|];

                        await Task.Yield();
                        return await Local();
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> Parent()
                    {
                        Task<int> Local() => Inner();

                        await Task.Yield();
                        return await Local();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NestedLambdaAwait_DoesNotAffectParent()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> Parent()
                    {
                        Func<Task> f = async () => await Task.Yield();
                        _ = f();
                        return [|await Inner()|];
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System;
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    Task<int> Parent()
                    {
                        Func<Task> f = async () => await Task.Yield();
                        _ = f();
                        return Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task OtherAwaitInMethod_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> A()
                    {
                        await Task.Yield();
                        return await Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task SomeReturnsNotAwaited_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> A(bool condition)
                    {
                        if (condition)
                            return await Inner();

                        return 0;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ReturnAwaitInUsingBlock_NoDiagnostic()
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
    public async Task UsingDeclarationInChildBlock_OutOfScope()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    async Task<int> A(bool condition)
                    {
                        if (condition)
                        {
                            using var d = (IDisposable)null;
                            d.ToString();
                        }

                        return [|await Inner()|];
                    }
                }
                """)
            .ShouldFixCodeWith("""
                using System;
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner() => throw null;
                    Task<int> A(bool condition)
                    {
                        if (condition)
                        {
                            using var d = (IDisposable)null;
                            d.ToString();
                        }

                        return Inner();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ReturnAwaitWithUsingDeclaration_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;
                using System.Threading.Tasks;
                class Test
                {
                    Task<int> Inner(IDisposable d) => throw null;
                    async Task<int> A()
                    {
                        using var d = (IDisposable)null;
                        return await Inner(d);
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

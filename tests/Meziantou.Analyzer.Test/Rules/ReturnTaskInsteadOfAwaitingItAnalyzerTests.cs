using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.ReturnTaskInsteadOfAwaitingItAnalyzer,
    Meziantou.Analyzer.Rules.ReturnTaskInsteadOfAwaitingItFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ReturnTaskInsteadOfAwaitingItAnalyzerTests
{
    private static CodeFixTest CreateTest() => new() { ReferenceAssemblies = ReferenceAssemblies.Net.Net80 };

    [Fact]
    public void Rule_IsDisabledByDefault()
    {
        var rule = new ReturnTaskInsteadOfAwaitingItAnalyzer().SupportedDiagnostics[0];
        Assert.False(rule.IsEnabledByDefault);
    }

    [Fact]
    public Task Issue1280_StaticVoidTaskWithConfigureAwaitAndLambdaArgument()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class Test
            {
                static Task InvokeDialogActionAsync(Action action) => throw null;

                internal static async Task CloseNotifyIconAsync()
                {
                    {|MA0215:await InvokeDialogActionAsync(() => { }).ConfigureAwait(false)|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReturnAwait_TaskOfT()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task<int> A()
                {
                    return {|MA0215:await Inner()|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                Task<int> A()
                {
                    return Inner();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExpressionBody_TaskOfT()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task<int> A() => {|MA0215:await Inner()|};
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                Task<int> A() => Inner();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Await_NonGenericTask_AddsReturn()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task Inner() => throw null;
                async Task A()
                {
                    {|MA0215:await Inner()|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task Inner() => throw null;
                Task A()
                {
                    return Inner();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExpressionBody_NonGenericTask()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task Inner() => throw null;
                async Task A() => {|MA0215:await Inner()|};
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task Inner() => throw null;
                Task A() => Inner();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConfigureAwait_IsStripped()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task Inner() => throw null;
                async Task A()
                {
                    {|MA0215:await Inner().ConfigureAwait(false)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task Inner() => throw null;
                Task A()
                {
                    return Inner();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExpressionBody_ValueTaskOfT()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                ValueTask<int> Inner() => throw null;
                async ValueTask<int> A() => {|MA0215:await Inner()|};
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                ValueTask<int> Inner() => throw null;
                ValueTask<int> A() => Inner();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConfigureAwaitTrue_Generic_IsStripped()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task<int> A()
                {
                    return {|MA0215:await Inner().ConfigureAwait(true)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                Task<int> A()
                {
                    return Inner();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StaticLocalFunction()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                static Task<int> Inner() => throw null;
                void A()
                {
                    static async Task<int> Local() => {|MA0215:await Inner()|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                static Task<int> Inner() => throw null;
                void A()
                {
                    static Task<int> Local() => Inner();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SimpleLambda()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner(int x) => throw null;
                void A()
                {
                    Func<int, Task<int>> f = async x => {|MA0215:await Inner(x)|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AnonymousMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                void A()
                {
                    Func<Task<int>> f = async delegate { return {|MA0215:await Inner()|}; };
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NestedAwaitInOperand_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CovariantReturnTypeMismatch_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<string> Inner() => throw null;
                async Task<object> A()
                {
                    return await Inner();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CustomAwaitableNotAssignableToTask_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                async Task A()
                {
                    await Task.Yield();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitAssignedToDiscard_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task A()
                {
                    _ = await Inner();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalFunction()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                void A()
                {
                    async Task<int> Local() => {|MA0215:await Inner()|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                void A()
                {
                    Task<int> Local() => Inner();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Lambda()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                void A()
                {
                    Func<Task<int>> f = async () => {|MA0215:await Inner()|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleMethods_BatchFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                Task Inner2() => throw null;
                async Task<int> A() => {|MA0215:await Inner()|};
                async Task B() => {|MA0215:await Inner2()|};
                async Task<int> C()
                {
                    return {|MA0215:await Inner()|};
                }
                async Task D()
                {
                    {|MA0215:await Inner2()|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrecedingStatement()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task<int> A()
                {
                    System.Console.WriteLine();
                    return {|MA0215:await Inner()|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleReturns_TaskOfT()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task<int> A(bool condition)
                {
                    if (condition)
                        return {|MA0215:await Inner()|};

                    return {|MA0215:await Inner()|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NestedLocalFunctionAwait_DoesNotAffectParent()
    {
        var test = CreateTest();
        test.TestCode = """
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

                    return {|MA0215:await Local()|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalFunctionReported_ParentIsNot()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task<int> Parent()
                {
                    async Task<int> Local() => {|MA0215:await Inner()|};

                    await Task.Yield();
                    return await Local();
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NestedLambdaAwait_DoesNotAffectParent()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task<int> Parent()
                {
                    Func<Task> f = async () => await Task.Yield();
                    _ = f();
                    return {|MA0215:await Inner()|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OtherAwaitInMethod_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SomeReturnsNotAwaited_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReturnAwaitInUsingBlock_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UsingDeclarationInChildBlock_OutOfScope()
    {
        var test = CreateTest();
        test.TestCode = """
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

                    return {|MA0215:await Inner()|};
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReturnAwaitWithUsingDeclaration_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InsideTry_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InsideUsing_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleAwaits_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task<int> A()
                {
                    return await Inner() + await Inner();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAssignable_ValueTaskAwaitingTask_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task Inner() => throw null;
                async ValueTask A()
                {
                    await Inner();
                }
            }
            """;

        return test.RunAsync();
    }
}

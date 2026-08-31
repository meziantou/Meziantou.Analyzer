using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseAwaitInsteadOfReturningTaskAnalyzer,
    Meziantou.Analyzer.Rules.UseAwaitInsteadOfReturningTaskFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseAwaitInsteadOfReturningTaskAnalyzerTests
{
    private static CodeFixTest CreateTest() => new() { ReferenceAssemblies = ReferenceAssemblies.Net.Net80 };

    [Fact]
    public void Rule_IsDisabledByDefault()
    {
        var rule = new UseAwaitInsteadOfReturningTaskAnalyzer().SupportedDiagnostics[0];
        Assert.False(rule.IsEnabledByDefault);
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
                Task<int> A() => {|MA0214:Inner()|};
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task<int> A() => await Inner();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task BlockBody_TaskOfT()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                Task<int> A()
                {
                    return {|MA0214:Inner()|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task<int> A()
                {
                    return await Inner();
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
                Task A() => {|MA0214:Inner()|};
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task Inner() => throw null;
                async Task A() => await Inner();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task BlockBody_NonGenericTask_DropsReturn()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task Inner() => throw null;
                Task A()
                {
                    return {|MA0214:Inner()|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task Inner() => throw null;
                async Task A()
                {
                    await Inner();
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
                    Task<int> Local() => {|MA0214:Inner()|};
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
                    async Task<int> Local() => await Inner();
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
                    Func<Task<int>> f = () => {|MA0214:Inner()|};
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
                    Func<Task<int>> f = async () => await Inner();
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
                ValueTask<int> A() => {|MA0214:Inner()|};
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                ValueTask<int> Inner() => throw null;
                async ValueTask<int> A() => await Inner();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExpressionBody_NonGenericValueTask()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                ValueTask Inner() => throw null;
                ValueTask A() => {|MA0214:Inner()|};
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                ValueTask Inner() => throw null;
                async ValueTask A() => await Inner();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MethodWithAccessModifier()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                public Task<int> A() => {|MA0214:Inner()|};
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                public async Task<int> A() => await Inner();
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
                    static Task<int> Local() => {|MA0214:Inner()|};
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
                    static async Task<int> Local() => await Inner();
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
                    Func<int, Task<int>> f = x => {|MA0214:Inner(x)|};
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
                    Func<int, Task<int>> f = async x => await Inner(x);
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
                    Func<Task<int>> f = delegate { return {|MA0214:Inner()|}; };
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
                    Func<Task<int>> f = async delegate { return await Inner(); };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyGetter_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                Task<int> P
                {
                    get { return Inner(); }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Operator_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                static Task<int> Inner() => throw null;
                public static Task<int> operator +(Test a, Test b) => Inner();
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
                Task<int> A() => {|MA0214:Inner()|};
                Task B() => {|MA0214:Inner2()|};
                Task<int> C()
                {
                    return {|MA0214:Inner()|};
                }
                Task D()
                {
                    return {|MA0214:Inner2()|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                Task Inner2() => throw null;
                async Task<int> A() => await Inner();
                async Task B() => await Inner2();
                async Task<int> C()
                {
                    return await Inner();
                }
                async Task D()
                {
                    await Inner2();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReturnInsideLock_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                private readonly object _gate = new();
                Task Inner() => throw null;
                Task A()
                {
                    lock (_gate)
                    {
                        return Inner();
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalFunctionReported_ParentAsyncIsNot()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task<int> Parent()
                {
                    Task<int> Local() => {|MA0214:Inner()|};
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
                    async Task<int> Local() => await Inner();
                    return await Local();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NestedLocalFunctionReturn_DoesNotAffectParent()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                Task<int> Parent()
                {
                    Task<int> Local() => null;
                    _ = Local();
                    return {|MA0214:Inner()|};
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
                    Task<int> Local() => null;
                    _ = Local();
                    return await Inner();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NestedLambdaReturn_DoesNotAffectParent()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                Task<int> Parent()
                {
                    Func<Task<int>> f = () => null;
                    _ = f();
                    return {|MA0214:Inner()|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task<int> Parent()
                {
                    Func<Task<int>> f = () => null;
                    _ = f();
                    return await Inner();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AlreadyAsync_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task<int> A() => await Inner();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReturnNull_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task A() => null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrecedingStatement_TaskOfT()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                Task<int> A()
                {
                    System.Console.WriteLine();
                    return {|MA0214:Inner()|};
                }
            }
            """;
        test.FixedCode = """
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
                Task<int> A(bool condition)
                {
                    if (condition)
                        return {|MA0214:Inner()|};

                    return {|MA0214:Inner()|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                async Task<int> A(bool condition)
                {
                    if (condition)
                        return await Inner();

                    return await Inner();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleReturns_NonGenericTask()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task Inner() => throw null;
                Task A(bool condition)
                {
                    if (condition)
                        return {|MA0214:Inner()|};

                    return {|MA0214:Inner()|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task Inner() => throw null;
                async Task A(bool condition)
                {
                    if (condition)
                    {
                        await Inner();
                        return;
                    }

                    await Inner();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SomeReturnsNotAwaitable_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> Inner() => throw null;
                Task<int> A(bool condition)
                {
                    if (condition)
                        return Inner();

                    return null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonAwaitableReturn_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                int A() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AsyncEnumerable_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class Test
            {
                IAsyncEnumerable<int> Inner() => throw null;
                IAsyncEnumerable<int> A() => Inner();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExpressionTree_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }
}

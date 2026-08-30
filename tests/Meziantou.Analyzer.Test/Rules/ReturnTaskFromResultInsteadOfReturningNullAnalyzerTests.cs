using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.ReturnTaskFromResultInsteadOfReturningNullAnalyzer,
    Meziantou.Analyzer.Rules.ReturnTaskFromResultInsteadOfReturningNullFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ReturnTaskFromResultInsteadOfReturningNullAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task MethodFixer_Task_Completed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task A() { [|return null;|] }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task A() { return Task.CompletedTask; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MethodFixer_Task_Completed2()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task A() => [|null|];
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task A() => Task.CompletedTask;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MethodFixer_Task_FromResult()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> A() { [|return null;|] }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<int> A() { return Task.FromResult(0); }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MethodFixer_Task_FromResult2()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<string> A() => [|null|];
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task<string> A() => Task.FromResult<string>(null);
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Task A() { [|return null;|] }")]
    [InlineData("Task A() { [|return default;|] }")]
    [InlineData("Task A() => [|null|];")]
    [InlineData("Task A() => [|default|];")]
    [InlineData("Task A() { [|return ((Test)null)?.A();|] }")]
    [InlineData("Task A() { [|return 1 switch { _ => null };|] }")]
    [InlineData("Task A(int value) { [|return value switch { 1 => A(0), _ => null };|] }")]
    [InlineData("Task A(bool a) { [|return a ? null : A(a);|] }")]
    [InlineData("Task<object> A() { [|return null;|] }")]
    [InlineData("Task<object> A() { [|return default;|] }")]
    [InlineData("Task<object> A() => [|null|];")]
    [InlineData("Task<object> A() => [|default|];")]
    [InlineData("async Task<object> Valid() { return null; }")]
    [InlineData("object Valid() { return null; }")]
    public Task Method(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Threading.Tasks;
            class Test
            {
                {{code}}
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
                void A()
                {
                    Task<object> Valid1() { return Task.FromResult<object>(null); }
                    async Task<object> Valid2() { return null; }
                    Task A() { [|return null;|] }
                    Task<object> B() { [|return null;|] }
                    Task<object> C() => [|null|];
                    object       D() => null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LambdaExpression()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    System.Func<Task>         a = () => [|null|];
                    System.Func<Task<object>> b = () => [|null|];
                    System.Func<Task<object>> c = () => { [|return null;|] };
                    System.Func<Task>         valid1 = async () => { };
                    System.Func<Task<object>> valid2 = async () => null;
                    System.Func<object>       valid3 = () => null;
                    System.Func<Task<object>> valid4 = async () => { return null; };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AnonymousMethods()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                void A()
                {
                    System.Func<Task> a = delegate () { [|return null;|] };
                    System.Func<Task<object>> b = delegate () { [|return null;|] };
                    System.Func<Task> c = async delegate () { };
                    System.Func<Task<object>> d = async delegate () { return null; };
                    System.Func<object> e = delegate () { return null; };
                    System.Action f = () => { };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AsyncLambdaInTask()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                Task A()
                {
                    System.Func<Task<object>> valid4 = async () => { return null; };
                    return Task.CompletedTask;
                }
            }
            """;

        return test.RunAsync();
    }
}

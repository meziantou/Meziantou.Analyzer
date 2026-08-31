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
                Task A() { {|MA0022:return null;|} }
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
                Task A() => {|MA0022:null|};
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
                Task<int> A() { {|MA0022:return null;|} }
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
                Task<string> A() => {|MA0022:null|};
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
    [InlineData("Task A() { {|MA0022:return null;|} }")]
    [InlineData("Task A() { {|MA0022:return default;|} }")]
    [InlineData("Task A() => {|MA0022:null|};")]
    [InlineData("Task A() => {|MA0022:default|};")]
    [InlineData("Task A() { {|MA0022:return ((Test)null)?.A();|} }")]
    [InlineData("Task A() { {|MA0022:return 1 switch { _ => null };|} }")]
    [InlineData("Task A(int value) { {|MA0022:return value switch { 1 => A(0), _ => null };|} }")]
    [InlineData("Task A(bool a) { {|MA0022:return a ? null : A(a);|} }")]
    [InlineData("Task<object> A() { {|MA0022:return null;|} }")]
    [InlineData("Task<object> A() { {|MA0022:return default;|} }")]
    [InlineData("Task<object> A() => {|MA0022:null|};")]
    [InlineData("Task<object> A() => {|MA0022:default|};")]
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
                    Task A() { {|MA0022:return null;|} }
                    Task<object> B() { {|MA0022:return null;|} }
                    Task<object> C() => {|MA0022:null|};
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
                    System.Func<Task>         a = () => {|MA0022:null|};
                    System.Func<Task<object>> b = () => {|MA0022:null|};
                    System.Func<Task<object>> c = () => { {|MA0022:return null;|} };
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
                    System.Func<Task> a = delegate () { {|MA0022:return null;|} };
                    System.Func<Task<object>> b = delegate () { {|MA0022:return null;|} };
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseTaskUnwrapAnalyzer,
    Meziantou.Analyzer.Rules.UseTaskUnwrapFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseTaskUnwrapAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task TaskOfTask()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;

            Task<Task> a = null;
            [|await await a|];
            """;
        test.FixedCode = """
            using System.Threading.Tasks;

            Task<Task> a = null;
            await a.Unwrap();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TaskOfTask_ConfigureAwait()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;

            Task<Task> a = null;
            await (await a.ConfigureAwait(false));
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TaskOfTask_ConfigureAwait_Root()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;

            Task<Task> a = null;
            [|await (await a).ConfigureAwait(false)|];
            """;
        test.FixedCode = """
            using System.Threading.Tasks;

            Task<Task> a = null;
            await a.Unwrap().ConfigureAwait(false);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TaskOfTask_Unwrap_ConfigureAwait_Root()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;

            Task<Task> a = null;
            await a.Unwrap().ConfigureAwait(false);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TaskOfTaskOfInt32()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;

            Task<Task<int>> a = null;
            int b = [|await await a|];
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TaskOfValueTaskOfInt32()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;

            Task<ValueTask<int>> a = null;
            int b = await await a;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValueTaskOfTaskOfInt32()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;

            ValueTask<Task<int>> a = default;
            int b = await await a;
            """;

        return test.RunAsync();
    }
}

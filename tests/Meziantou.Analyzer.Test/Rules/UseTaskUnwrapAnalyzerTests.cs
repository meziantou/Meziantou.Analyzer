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
            {|MA0152:await await a|};
            """;
        test.FixedCode = """
            using System.Threading.Tasks;

            Task<Task> a = null;
            await a.Unwrap();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TaskOfTask_WithoutUsingDirective()
    {
        var test = CreateTest();
        test.TestCode = """
            System.Threading.Tasks.Task<System.Threading.Tasks.Task> a = null;
            {|MA0152:await await a|};
            """;
        test.FixedCode = """
            using System.Threading.Tasks;

            System.Threading.Tasks.Task<System.Threading.Tasks.Task> a = null;
            await a.Unwrap();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TaskOfTask_WithoutUsingDirective_InNamespace()
    {
        var test = new CodeFixTest
        {
            TestCode = """
                namespace Sample;

                public class Test
                {
                    public static async System.Threading.Tasks.Task Run(System.Threading.Tasks.Task<System.Threading.Tasks.Task> a)
                    {
                        {|MA0152:await await a|};
                    }
                }
                """,
            FixedCode = """
                using System.Threading.Tasks;

                namespace Sample;

                public class Test
                {
                    public static async System.Threading.Tasks.Task Run(System.Threading.Tasks.Task<System.Threading.Tasks.Task> a)
                    {
                        await a.Unwrap();
                    }
                }
                """,
        };

        return test.RunAsync();
    }

    [Fact]
    public Task TaskOfTask_GlobalUsingDirective()
    {
        const string GlobalUsings = "global using System.Threading.Tasks;";
        var test = CreateTest();
        test.TestState.Sources.Add(("/0/Test0.cs", """
            Task<Task> a = null;
            {|MA0152:await await a|};
            """));
        test.TestState.Sources.Add(("/0/GlobalUsings.cs", GlobalUsings));
        test.FixedState.Sources.Add(("/0/Test0.cs", """
            Task<Task> a = null;
            await a.Unwrap();
            """));
        test.FixedState.Sources.Add(("/0/GlobalUsings.cs", GlobalUsings));

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
            {|MA0152:await (await a).ConfigureAwait(false)|};
            """;
        test.FixedCode = """
            using System.Threading.Tasks;

            Task<Task> a = null;
            await a.Unwrap().ConfigureAwait(false);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TaskOfTask_ConfigureAwait_Root_WithoutUsingDirective()
    {
        var test = CreateTest();
        test.TestCode = """
            System.Threading.Tasks.Task<System.Threading.Tasks.Task> a = null;
            {|MA0152:await (await a).ConfigureAwait(false)|};
            """;
        test.FixedCode = """
            using System.Threading.Tasks;

            System.Threading.Tasks.Task<System.Threading.Tasks.Task> a = null;
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
            int b = {|MA0152:await await a|};
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

using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseTaskUnwrapAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseTaskUnwrapAnalyzer>()
            .WithTargetFramework(TargetFramework.Net6_0)
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication);
    }

    [Fact]
    public async Task TaskOfTask()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;

                Task<Task> a = null;
                [|await await a|];
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TaskOfTask_ConfigureAwait()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;

                Task<Task> a = null;
                await (await a.ConfigureAwait(false));
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TaskOfTask_ConfigureAwait_Root()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;

                Task<Task> a = null;
                [|await (await a).ConfigureAwait(false)|];
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TaskOfTask_Unwrap_ConfigureAwait_Root()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;

                Task<Task> a = null;
                await a.Unwrap().ConfigureAwait(false);
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TaskOfTaskOfInt32()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;

                Task<Task<int>> a = null;
                int b = [|await await a|];
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TaskOfValueTaskOfInt32()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;

                Task<ValueTask<int>> a = null;
                int b = await await a;
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ValueTaskOfTaskOfInt32()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Threading.Tasks;

                ValueTask<Task<int>> a = default;
                int b = await await a;
                """)
            .ValidateAsync();
    }
}

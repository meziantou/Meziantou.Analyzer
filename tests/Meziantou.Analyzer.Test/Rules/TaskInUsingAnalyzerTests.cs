using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class TaskInUsingAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithOutputKind(OutputKind.ConsoleApplication)
            .WithAnalyzer<TaskInUsingAnalyzer>();
    }

    [Fact]
    public async Task SingleTaskInUsing()
    {
        const string SourceCode = """
            using System.Threading.Tasks;
            
            Task t = null;
            using ([|t|]) { }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SingleTaskAssignedInUsing()
    {
        const string SourceCode = """
            using System.Threading.Tasks;
            
            Task t = null;
            using (var a = [|t|]) { }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultipleTasksInUsing()
    {
        const string SourceCode = """
            using System.Threading.Tasks;
            
            Task t1 = null;
            Task t2 = null;
            using (Task a = [|t1|], b = [|t2|]) { }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task TaskOfTInUsing()
    {
        const string SourceCode = """
            using System.Threading.Tasks;

            Task<System.IDisposable> t1 = null;
            using ([|t1|]) { }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task TaskOfTInUsingStatement()
    {
        const string SourceCode = """
            using System.Threading.Tasks;

            Task<System.IDisposable> t1 = null;
            using var a = [|t1|];
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}

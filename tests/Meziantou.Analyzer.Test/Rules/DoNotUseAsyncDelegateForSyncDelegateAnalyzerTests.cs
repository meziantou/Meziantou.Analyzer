using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseAsyncDelegateForSyncDelegateAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotUseAsyncDelegateForSyncDelegateAnalyzer>()
            .WithTargetFramework(TargetFramework.Net8_0)
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication);
    }

    [Fact]
    public async Task List_ForEach_Sync()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  new System.Collections.Generic.List<int>().ForEach(item => {});
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task List_ForEach_Async()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  new System.Collections.Generic.List<int>().ForEach([|async item => {}|]);
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CustomDelegate_Sync()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  A(() => {});

                  void A(D a) => throw null;
                  delegate void D();
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CustomDelegate_AsyncVoid()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  A([|async () => {}|]);

                  void A(D a) => throw null;
                  delegate void D();
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Action_Sync()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  A(() => {});

                  void A(System.Action a) => throw null;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Action_AsyncVoid()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  A([|async () => {}|]);

                  void A(System.Action a) => throw null;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task FuncTask_AsyncDelegate()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  A(async () => {});

                  void A(System.Func<System.Threading.Tasks.Task> a) => throw null;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task FuncValueTask_AsyncDelegate()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  A(async () => {});

                  void A(System.Func<System.Threading.Tasks.ValueTask> a) => throw null;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task FuncValueTaskOfInt_AsyncDelegate()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  A(async () => 1);

                  void A(System.Func<System.Threading.Tasks.ValueTask<int>> a) => throw null;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Event_AsyncVoid()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  Sample.A += async (sender, e) => { };
                  Sample.A -= async (sender, e) => { };

                  class Sample
                  {
                      public static event System.EventHandler A;
                  }
                  """)
              .ValidateAsync();
    }
}

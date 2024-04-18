using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class DoNotUseAsyncVoidAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotUseAsyncVoidAnalyzer>()
            .WithTargetFramework(TargetFramework.Net8_0);
    }

    [Fact]
    public async Task Method_Void()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      void A() => throw null;
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Method_AsyncVoid()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      async void [|A|]() => throw null;
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Method_AsyncTask()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      async System.Threading.Tasks.Task A() => throw null;
                  }
                  """)
              .ValidateAsync();
    }

}

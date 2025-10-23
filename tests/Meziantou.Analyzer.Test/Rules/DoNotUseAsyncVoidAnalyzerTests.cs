using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

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

    [Fact]
    public async Task LocalFunction_Void()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      void A()
                      {
                        void Local() => throw null;
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LocalFunction_AsyncVoid()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      void A()
                      {
                        [|async void Local() => throw null;|]
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task LocalFunction_AsyncTask()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      void A()
                      {
                        async System.Threading.Tasks.Task Local() => throw null;
                      }
                  }
                  """)
              .ValidateAsync();
    }
}

#if ROSLYN_4_8_OR_GREATER
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public class UseReadOnlyStructForRefReadOnlyParametersAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithAnalyzer<UseReadOnlyStructForRefReadOnlyParametersAnalyzer>();
    }

    [Fact]
    public async Task ParameterNotRefReadOnly()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  A(default);

                  void A(Foo foo) { }
                  struct Foo { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task StructNotReadOnly_in()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""              
                  A(default);
                  
                  void A(in Foo [|foo|]) { }
                  struct Foo { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task StructNotReadOnly_ref_readonly()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""              
                  A(default);
                  
                  void A(ref readonly Foo [|foo|]) { }
                  struct Foo { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task StructReadOnly()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""              
                  A(default);
                  
                  void A(in Foo foo) { }
                  readonly struct Foo { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task StructNotReadOnly_Generic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""              
                  A([|new Foo()|]);
                  
                  void A<T>(in T foo) where T: struct { }
                  struct Foo { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task StructReadOnly_Generic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""              
                  A(new Foo());
                  
                  void A<T>(in T foo) where T: struct { }
                  readonly struct Foo { }
                  """)
              .ValidateAsync();
    }
}
#endif
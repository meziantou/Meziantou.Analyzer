using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;
public class JSInvokableMethodsMustBePublicAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<JSInvokableMethodsMustBePublicAnalyzer>()
            .WithTargetFramework(TargetFramework.AspNetCore6_0);
    }

    [Fact]
    public async Task Test()
    {
        const string SourceCode = """
            using Microsoft.JSInterop;
            
            class Test
            {
                [JSInvokable]
                public void A() => throw null;
            
                [JSInvokable]
                internal void [||]B() => throw null;
            
                [JSInvokable]
                static void [||]C() => throw null;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}

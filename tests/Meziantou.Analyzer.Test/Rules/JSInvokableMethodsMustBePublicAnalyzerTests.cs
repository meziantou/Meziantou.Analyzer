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
            .WithCodeFixProvider<JSInvokableMethodsMustBePublicFixer>()
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
                internal void [|B|]() => throw null;

                [JSInvokable]
                static void [|C|]() => throw null;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_CodeFix_InternalMethod()
    {
        const string SourceCode = """
            using Microsoft.JSInterop;

            class Test
            {
                [JSInvokable]
                public void A() => throw null;

                [JSInvokable]
                internal void [|B|]() => throw null;
            }
            """;
        const string CodeFix = """
            using Microsoft.JSInterop;

            class Test
            {
                [JSInvokable]
                public void A() => throw null;

                [JSInvokable]
                public void B() => throw null;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_CodeFix_PrivateStaticMethod()
    {
        const string SourceCode = """
            using Microsoft.JSInterop;

            class Test
            {
                [JSInvokable]
                public void A() => throw null;

                [JSInvokable]
                private static void [|C|]() => throw null;
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith("""
                using Microsoft.JSInterop;

                class Test
                {
                    [JSInvokable]
                    public void A() => throw null;

                    [JSInvokable]
                    public static void C() => throw null;
                }
                """)
              .ValidateAsync();
    }
}

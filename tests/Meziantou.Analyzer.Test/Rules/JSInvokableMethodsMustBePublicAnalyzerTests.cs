using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.JSInvokableMethodsMustBePublicAnalyzer,
    Meziantou.Analyzer.Rules.JSInvokableMethodsMustBePublicFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class JSInvokableMethodsMustBePublicAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddAspNetCore();
        return test;
    }

    [Fact]
    public Task Test()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task Test_CodeFix_InternalMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.JSInterop;

            class Test
            {
                [JSInvokable]
                public void A() => throw null;

                [JSInvokable]
                internal void [|B|]() => throw null;
            }
            """;
        test.FixedCode = """
            using Microsoft.JSInterop;

            class Test
            {
                [JSInvokable]
                public void A() => throw null;

                [JSInvokable]
                public void B() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_CodeFix_PrivateStaticMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.JSInterop;

            class Test
            {
                [JSInvokable]
                public void A() => throw null;

                [JSInvokable]
                private static void [|C|]() => throw null;
            }
            """;
        test.FixedCode = """
            using Microsoft.JSInterop;

            class Test
            {
                [JSInvokable]
                public void A() => throw null;

                [JSInvokable]
                public static void C() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_CodeFix_StaticPrivateMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.JSInterop;

            class Test
            {
                [JSInvokable]
                static private void [|C|]() => throw null;
            }
            """;
        test.FixedCode = """
            using Microsoft.JSInterop;

            class Test
            {
                [JSInvokable]
                static public void C() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_CodeFix_StaticMethodWithoutVisibilityModifier()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.JSInterop;

            class Test
            {
                [JSInvokable]
                static void [|C|]() => throw null;
            }
            """;
        test.FixedCode = """
            using Microsoft.JSInterop;

            class Test
            {
                [JSInvokable]
                public static void C() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_CodeFix_PrivateProtectedStaticMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.JSInterop;

            class Test
            {
                [JSInvokable]
                private protected static void [|A|]() => throw null;
            }
            """;
        test.FixedCode = """
            using Microsoft.JSInterop;

            class Test
            {
                [JSInvokable]
                public static void A() => throw null;
            }
            """;

        return test.RunAsync();
    }
}

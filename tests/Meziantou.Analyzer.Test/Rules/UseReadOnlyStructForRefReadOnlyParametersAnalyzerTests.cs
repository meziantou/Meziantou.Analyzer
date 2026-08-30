using Microsoft.CodeAnalysis;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.UseReadOnlyStructForRefReadOnlyParametersAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public class UseReadOnlyStructForRefReadOnlyParametersAnalyzerTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task ParameterNotRefReadOnly()
    {
        var test = CreateTest();
        test.TestCode = """
            A(default);

            void A(Foo foo) { }
            struct Foo { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StructNotReadOnly_in()
    {
        var test = CreateTest();
        test.TestCode = """
            A(default);

            void A(in Foo [|foo|]) { }
            struct Foo { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StructNotReadOnly_ref_readonly()
    {
        var test = CreateTest();
        test.TestCode = """
            A(default);

            void A(ref readonly Foo [|foo|]) { }
            struct Foo { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StructReadOnly()
    {
        var test = CreateTest();
        test.TestCode = """
            A(default);

            void A(in Foo foo) { }
            readonly struct Foo { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StructNotReadOnly_Generic()
    {
        var test = CreateTest();
        test.TestCode = """
            A([|new Foo()|]);

            void A<T>(in T foo) where T: struct { }
            struct Foo { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StructReadOnly_Generic()
    {
        var test = CreateTest();
        test.TestCode = """
            A(new Foo());

            void A<T>(in T foo) where T: struct { }
            readonly struct Foo { }
            """;

        return test.RunAsync();
    }
}

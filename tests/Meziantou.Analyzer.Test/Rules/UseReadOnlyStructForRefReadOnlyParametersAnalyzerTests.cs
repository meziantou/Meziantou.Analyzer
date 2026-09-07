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

    /// <summary>
    /// The code of the tests using the members of a type is not a top-level program, so it has no entry point.
    /// </summary>
    private static AnalyzerTest CreateLibraryTest() => new();

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

            void A(in Foo {|MA0168:foo|}) { }
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

            void A(ref readonly Foo {|MA0168:foo|}) { }
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
            A({|MA0168:new Foo()|});

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

    [Fact]
    public Task Method_StructNotReadOnly_in()
    {
        var test = CreateLibraryTest();
        test.TestCode = """
            class Test
            {
                void A(in Foo {|MA0168:foo|}) { }
            }

            struct Foo { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Method_StructNotReadOnly_ref_readonly()
    {
        var test = CreateLibraryTest();
        test.TestCode = """
            class Test
            {
                void A(ref readonly Foo {|MA0168:foo|}) { }
            }

            struct Foo { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Method_StructReadOnly()
    {
        var test = CreateLibraryTest();
        test.TestCode = """
            class Test
            {
                void A(in Foo foo) { }
            }

            readonly struct Foo { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Constructor_StructNotReadOnly_in()
    {
        var test = CreateLibraryTest();
        test.TestCode = """
            class Test
            {
                Test(in Foo {|MA0168:foo|}) { }
            }

            struct Foo { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Indexer_StructNotReadOnly_in()
    {
        var test = CreateLibraryTest();
        test.TestCode = """
            class Test
            {
                int this[in Foo {|MA0168:foo|}] => 0;
            }

            struct Foo { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Delegate_StructNotReadOnly_in()
    {
        var test = CreateLibraryTest();
        test.TestCode = """
            delegate void A(in Foo {|MA0168:foo|});

            struct Foo { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Method_ParameterNotRefReadOnly()
    {
        var test = CreateLibraryTest();
        test.TestCode = """
            class Test
            {
                void A(Foo foo) { }
            }

            struct Foo { }
            """;

        return test.RunAsync();
    }
}

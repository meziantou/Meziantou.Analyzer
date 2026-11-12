using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.AbstractTypesShouldNotHaveConstructorsAnalyzer,
    Meziantou.Analyzer.Rules.AbstractTypesShouldNotHaveConstructorsFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AbstractTypesShouldNotHaveConstructorsAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task Ctor()
    {
        var test = CreateTest();
        test.TestCode = """
            abstract class Test
            {
                protected Test(int a) { }
                private Test(object a) { }
            }

            class Test2
            {
                public Test2() { }
                internal Test2(long a) { }
                protected Test2(int a) { }
                private Test2(object a) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PublicCtor()
    {
        var test = CreateTest();
        test.TestCode = """
            abstract class Test
            {
                public {|MA0017:Test|}() { }
            }
            """;
        test.FixedCode = """
            abstract class Test
            {
                protected Test() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalCtor()
    {
        var test = CreateTest();
        test.TestCode = """
            abstract class Test
            {
                internal {|MA0017:Test|}() { }
            }
            """;
        test.FixedCode = """
            abstract class Test
            {
                protected Test() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PublicCtor_KeepXmlDocumentation()
    {
        var test = CreateTest();
        test.TestCode = """
            abstract class Test
            {
                /// <summary>Creates an instance.</summary>
                public {|MA0017:Test|}() { }
            }
            """;
        test.FixedCode = """
            abstract class Test
            {
                /// <summary>Creates an instance.</summary>
                protected Test() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalCtor_WithOtherModifier()
    {
        var test = CreateTest();
        test.TestCode = """
            abstract class Test
            {
                // comment
                internal unsafe {|MA0017:Test|}(int* value) { }
            }
            """;
        test.FixedCode = """
            abstract class Test
            {
                // comment
                protected unsafe Test(int* value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InternalCtor_BatchFix()
    {
        var test = CreateTest();
        test.TestCode = """
            abstract class Test
            {
                internal {|MA0017:Test|}() { }

                internal {|MA0017:Test|}(int a) { }
            }
            """;
        test.FixedCode = """
            abstract class Test
            {
                protected Test() { }

                protected Test(int a) { }
            }
            """;

        return test.RunAsync();
    }
}

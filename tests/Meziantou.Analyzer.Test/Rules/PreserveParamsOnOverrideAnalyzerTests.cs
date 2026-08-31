using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.PreserveParamsOnOverrideAnalyzer,
    Meziantou.Analyzer.Rules.PreserveParamsOnOverrideFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class PreserveParamsOnOverrideAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task MissingParamsFromBaseClass()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                protected virtual void A(params string[] a) => throw null;
            }

            class Test2 : Test
            {
                protected override void A(string[] {|MA0081:a|}) => throw null;
            }
            """;
        test.FixedCode = """
            class Test
            {
                protected virtual void A(params string[] a) => throw null;
            }

            class Test2 : Test
            {
                protected override void A(params string[] a) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingParamsFromInterface()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
                void A(params string[] a);
            }

            class Test2 : ITest
            {
                public void A(string[] {|MA0081:a|}) => throw null;
            }
            """;
        test.FixedCode = """
            interface ITest
            {
                void A(params string[] a);
            }

            class Test2 : ITest
            {
                public void A(params string[] a) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParamsFromBaseClass()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                protected virtual void A(params string[] a) => throw null;
            }

            class Test2 : Test
            {
                protected override void A(params string[] a) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParamsFromInterface()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
                void A(params string[] a);
            }

            class Test2 : ITest
            {
                public void A(params string[] a) => throw null;
            }
            """;

        return test.RunAsync();
    }
}

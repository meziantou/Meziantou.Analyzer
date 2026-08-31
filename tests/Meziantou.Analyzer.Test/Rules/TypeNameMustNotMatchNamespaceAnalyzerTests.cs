using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.TypeNameMustNotMatchNamespaceAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class TypeNameMustNotMatchNamespaceAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task DifferentName()
    {
        var test = CreateTest();
        test.TestCode = """
            namespace TestNamespace
            {
                class TestClass
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SameName()
    {
        var test = CreateTest();
        test.TestCode = """
            namespace Test
            {
                class {|MA0049:Test|}
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SameNameInNestedType()
    {
        var test = CreateTest();
        test.TestCode = """
            namespace Test
            {
                class TestClass
                {
                    class Test
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }
}

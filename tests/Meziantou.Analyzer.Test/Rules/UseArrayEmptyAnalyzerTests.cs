using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseArrayEmptyAnalyzer,
    Meziantou.Analyzer.Rules.UseArrayEmptyFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseArrayEmptyAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Theory]
    [InlineData("new int[0]")]
    [InlineData("new int[] { }")]
    public Task EmptyArray_ShouldReportError(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                void Test()
                {
                    var a = {|MA0005:{{code}}|};
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test()
                {
                    var a = System.Array.Empty<int>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("new int[1]")]
    [InlineData("new int[] { 0 }")]
    public Task NonEmptyArray_ShouldReportError(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                void Test()
                {
                    var a = {{code}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Length_FlowedFromLocal_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    int length = 0;
                    var a = {|MA0005:new int[length]|};
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test()
                {
                    int length = 0;
                    var a = System.Array.Empty<int>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParamsMethod_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            public class TestClass
            {
                public void Test(params string[] values)
                {
                }

                public void CallTest()
                {
                    Test();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyArrayInAttribute_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            [Test(new int[0])]
            class TestAttribute : System.Attribute
            {
                public TestAttribute(int[] data) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitEmptyArrayInAttribute_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            [Test("test")]
            class TestAttribute : System.Attribute
            {
                public TestAttribute(string a, params object[] data) { }
            }
            """;

        return test.RunAsync();
    }
}

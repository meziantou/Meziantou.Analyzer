using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseGuidEmptyAnalyzer,
    Meziantou.Analyzer.Rules.UseGuidEmptyFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseGuidEmptyAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Theory]
    [InlineData("new System.Guid()")]
    [InlineData("new System.Guid(\"00000000-0000-0000-0000-000000000000\")")]
    [InlineData("new System.Guid(\"{00000000-0000-0000-0000-000000000000}\")")]
    [InlineData("new System.Guid(\"00000000000000000000000000000000\")")]
    [InlineData("new System.Guid(\"(00000000-0000-0000-0000-000000000000)\")")]
    [InlineData("new System.Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)")]
    [InlineData("System.Guid.Parse(\"00000000-0000-0000-0000-000000000000\")")]
    [InlineData("System.Guid.Parse(\"{00000000-0000-0000-0000-000000000000}\")")]
    [InlineData("System.Guid.Parse(\"00000000000000000000000000000000\")")]
    [InlineData("System.Guid.Parse(\"(00000000-0000-0000-0000-000000000000)\")")]
    public Task ShouldReportError(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                void Test()
                {
                    _ = {|MA0067:{{code}}|};
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test()
                {
                    _ = System.Guid.Empty;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ShouldReportError_FlowedFromLocal()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    var value = "00000000-0000-0000-0000-000000000000";
                    _ = {|MA0067:System.Guid.Parse(value)|};
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test()
                {
                    var value = "00000000-0000-0000-0000-000000000000";
                    _ = System.Guid.Empty;
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("new System.Guid(\"\")")]
    [InlineData("new System.Guid(\"10752bc4-c151-50f5-f27b-df92d8af5a61\")")]
    [InlineData("System.Guid.Parse(\"10752bc4-c151-50f5-f27b-df92d8af5a61\")")]
    [InlineData("new System.Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)")]
    public Task ShouldNotReportError(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                void Test()
                {
                    _ = {{code}};
                }
            }
            """;

        return test.RunAsync();
    }
}

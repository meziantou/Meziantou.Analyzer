using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseTimeSpanZeroAnalyzer,
    Meziantou.Analyzer.Rules.UseTimeSpanZeroFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseTimeSpanZeroAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Theory]
    [InlineData("System.TimeSpan.FromSeconds(0)")]
    [InlineData("System.TimeSpan.FromSeconds(0.0)")]
    [InlineData("System.TimeSpan.FromMinutes(0)")]
    [InlineData("System.TimeSpan.FromMinutes(0.0)")]
    [InlineData("System.TimeSpan.FromHours(0)")]
    [InlineData("System.TimeSpan.FromHours(0.0)")]
    [InlineData("System.TimeSpan.FromDays(0)")]
    [InlineData("System.TimeSpan.FromDays(0.0)")]
    [InlineData("System.TimeSpan.FromMilliseconds(0)")]
    [InlineData("System.TimeSpan.FromMilliseconds(0L)")]
    [InlineData("System.TimeSpan.FromMilliseconds(0L, 0L)")]
    [InlineData("System.TimeSpan.FromMilliseconds(0.0)")]
    [InlineData("System.TimeSpan.FromMilliseconds(0.0d)")]
    [InlineData("System.TimeSpan.FromMicroseconds(0)")]
    [InlineData("System.TimeSpan.FromMicroseconds(0.0)")]
    [InlineData("System.TimeSpan.FromTicks(0)")]
    [InlineData("System.TimeSpan.FromTicks(0L)")]
    public Task ShouldReportDiagnostic(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                void Test()
                {
                    _ = [|{{code}}|];
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test()
                {
                    _ = System.TimeSpan.Zero;
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("System.TimeSpan.FromSeconds(1)")]
    [InlineData("System.TimeSpan.FromSeconds(0.5)")]
    [InlineData("System.TimeSpan.FromMinutes(1)")]
    [InlineData("System.TimeSpan.FromHours(1)")]
    [InlineData("System.TimeSpan.FromDays(1)")]
    [InlineData("System.TimeSpan.FromMilliseconds(100)")]
    [InlineData("System.TimeSpan.FromMicroseconds(1)")]
    [InlineData("System.TimeSpan.FromTicks(1)")]
    [InlineData("System.TimeSpan.Zero")]
    [InlineData("new System.TimeSpan()")]
    [InlineData("new System.TimeSpan(0)")]
    [InlineData("default(System.TimeSpan)")]
    public Task ShouldNotReportDiagnostic(string code)
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

    [Fact]
    public Task ShouldReportDiagnostic_MultipleOccurrences()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    _ = [|System.TimeSpan.FromSeconds(0)|];
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test()
                {
                    _ = System.TimeSpan.Zero;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ShouldReportDiagnostic_FlowedFromLocal()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    var value = 0;
                    _ = [|System.TimeSpan.FromSeconds(value)|];
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test()
                {
                    var value = 0;
                    _ = System.TimeSpan.Zero;
                }
            }
            """;

        return test.RunAsync();
    }
}

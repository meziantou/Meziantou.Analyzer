using Microsoft.CodeAnalysis;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptimizeGuidCreationAnalyzer,
    Meziantou.Analyzer.Rules.OptimizeGuidCreationFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeGuidParsingAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task CtorConstantString()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = [|new System.Guid("10752bc4-c151-50f5-f27b-df92d8af5a61")|];
            """;
        test.FixedCode = """
            _ = new System.Guid(0x10752bc4, 0xc151, 0x50f5, 0xf2, 0x7b, 0xdf, 0x92, 0xd8, 0xaf, 0x5a, 0x61) /* 10752bc4-c151-50f5-f27b-df92d8af5a61 */;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParseConstantString()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = [|System.Guid.Parse("10752BC4-C151-50F5-F27B-DF92D8AF5A61")|];
            """;
        test.FixedCode = """
            _ = new System.Guid(0x10752BC4, 0xC151, 0x50F5, 0xF2, 0x7B, 0xDF, 0x92, 0xD8, 0xAF, 0x5A, 0x61) /* 10752BC4-C151-50F5-F27B-DF92D8AF5A61 */;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParseNonConstantString()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = "10752BC4-C151-50F5-F27B-DF92D8AF5A61";
            _ = System.Guid.Parse(value);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParseInvalidGuid()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = System.Guid.Parse("dummy");
            """;

        return test.RunAsync();
    }
}

using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseEventArgsEmptyAnalyzer,
    Meziantou.Analyzer.Rules.UseEventArgsEmptyFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseEventArgsEmptyAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = {|MA0019:new System.EventArgs()|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = System.EventArgs.Empty;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Attribute_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.EventArgs]
            class TypeName
            {
            }
            """;
        test.CompilerDiagnostics = CompilerDiagnostics.None;

        return test.RunAsync();
    }

    [Fact]
    public Task Attribute_InvalidCode_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            delegate d Fi[ System.EventArgs
            """;
        test.CompilerDiagnostics = CompilerDiagnostics.None;

        return test.RunAsync();
    }
}

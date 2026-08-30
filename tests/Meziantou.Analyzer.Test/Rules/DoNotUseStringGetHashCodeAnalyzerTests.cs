using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.DoNotUseStringGetHashCodeAnalyzer,
    Meziantou.Analyzer.Rules.DoNotUseStringGetHashCodeFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseStringGetHashCodeAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task GetHashCode_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    [|"a".GetHashCode()|];
                    System.StringComparer.Ordinal.GetHashCode("a");
                    new object().GetHashCode();
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    System.StringComparer.Ordinal.GetHashCode("a");
                    System.StringComparer.Ordinal.GetHashCode("a");
                    new object().GetHashCode();
                }
            }
            """;

        return test.RunAsync();
    }
}

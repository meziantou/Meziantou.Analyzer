using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.TypesShouldNotExtendSystemApplicationExceptionAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class TypesShouldNotExtendSystemApplicationExceptionAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task InheritFromException_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = "class Test : System.Exception { }";

        return test.RunAsync();
    }

    [Fact]
    public Task InheritFromApplicationException_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = "class [|Test|] : System.ApplicationException { }";

        return test.RunAsync();
    }
}

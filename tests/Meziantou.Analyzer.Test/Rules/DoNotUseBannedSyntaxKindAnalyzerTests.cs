using Meziantou.Analyzer.Test.Harness;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseBannedSyntaxKindAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseBannedSyntaxKindAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task NoConfiguration_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    goto label;
                    label:
                    return;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SingleKind_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.syntax_kinds", "GotoStatement");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    [|goto label;|]
                    label:
                    return;
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("GotoStatement, LockStatement")]
    [InlineData("GotoStatement LockStatement")]
    [InlineData(" gotostatement , LOCKSTATEMENT ")]
    public Task MultipleKinds_Diagnostic(string value)
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.syntax_kinds", value);
        test.TestCode = """
            class Sample
            {
                void M(object o)
                {
                    [|lock (o) { }|]
                    [|goto label;|]
                    label:
                    return;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NestedNodes_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.syntax_kinds", "ConditionalExpression");
        test.TestCode = """
            class Sample
            {
                int M(bool a, bool b) => [|a ? [|b ? 1 : 2|] : 3|];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Message_ContainsKind()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.syntax_kinds", "GotoStatement");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#0:goto label;|}
                    label:
                    return;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0240", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("GotoStatement"));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData("UnknownKind")]
    [InlineData("None")]
    [InlineData("8743")]
    [InlineData("-1")]
    public Task InvalidOrUnknownKinds_NoDiagnostic(string value)
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.syntax_kinds", value);
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    goto label;
                    label:
                    return;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnknownKindIsIgnored_OtherKindsAreReported()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.syntax_kinds", "UnknownKind, GotoStatement");
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    [|goto label;|]
                    label:
                    return;
                }
            }
            """;

        return test.RunAsync();
    }
}

using Meziantou.Analyzer.Test.Harness;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseBannedSyntaxKindAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseBannedSyntaxKindAnalyzerTests
{
    // The rule reports the invalid queries with a second descriptor, so the markup must use the first one
    private static AnalyzerTest CreateTest() => new() { MarkupOptions = MarkupOptions.UseFirstDescriptor };

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

    [Fact]
    public Task Query_PrimaryConstructor_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.query", "//ClassDeclaration/ParameterList");
        test.TestCode = """
            class Sample[|(int value)|]
            {
                Sample() : this(0) { }

                void M(int value) { }
            }

            class Other { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_Predicate_ReportsSelectedNode()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.query", "//ClassDeclaration[ParameterList]");
        test.TestCode = """
            [|class Sample(int value)
            {
            }|]

            class Other { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_MultipleQueries_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.query", "//ClassDeclaration/ParameterList | //StructDeclaration/ParameterList | //GotoStatement");
        test.TestCode = """
            class Sample[|(int value)|]
            {
                void M()
                {
                    [|goto label;|]
                    label:
                    return;
                }
            }

            struct S[|(int value)|];

            record R(int Value);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_NestedNodes_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.query", "//ConditionalExpression//ConditionalExpression");
        test.TestCode = """
            class Sample
            {
                int M(bool a, bool b) => a ? [|b ? 1 : 2|] : 3;
                int N(bool a) => a ? 1 : 2;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_TokenAttribute_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.query", "//MethodDeclaration[@Identifier='Banned']");
        test.TestCode = """
            class Sample
            {
                [|void Banned() { }|]
                void Allowed() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_TokenListAttribute_ReportsTokens()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.query", "//MethodDeclaration/@Modifiers[contains(., 'async')]");
        test.TestCode = """
            class Sample
            {
                {|#0:public async|} System.Threading.Tasks.Task A() => await System.Threading.Tasks.Task.Yield();
                public void B() { }
                void C() { }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0240", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("MethodDeclaration/@Modifiers"));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_ParentAxis_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.query", "//ReturnStatement/parent::Block/parent::MethodDeclaration/@Identifier");
        test.TestCode = """
            class Sample
            {
                int [|A|]()
                {
                    return 0;
                }

                void B() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SiblingAxes_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.query", "//MethodDeclaration[preceding-sibling::FieldDeclaration and following-sibling::FieldDeclaration]/@Identifier");
        test.TestCode = """
            class Sample
            {
                int a;
                void [|A|]() { }
                int b;
                void B() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task QueryAndSyntaxKinds_SameNode_ReportedOnce()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration(("MA0240.syntax_kinds", "GotoStatement"), ("MA0240.query", "//GotoStatement | //LockStatement"));
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
    public Task Query_NoMatch_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.query", "//GotoStatement");
        test.TestCode = """
            class Sample
            {
                void M() { }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("//ClassDeclaration[")]
    [InlineData("count(//ClassDeclaration)")]
    [InlineData("//ClassDeclaration[unknown-function()]")]
    public Task Query_Invalid_Diagnostic(string query)
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.query", query);
        test.TestCode = """
            class Sample
            {
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0240", DiagnosticSeverity.Warning));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_Count_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0240.query", "//*[count(ParameterList/Parameter) > 5]/@Identifier");
        test.TestCode = """
            class Sample
            {
                void [|A|](int a, int b, int c, int d, int e, int f) { }
                void B(int a, int b, int c, int d, int e) { }

                [|Sample|](int a, int b, int c, int d, int e, int f) { }

                void M()
                {
                    void [|Local|](int a, int b, int c, int d, int e, int f) { }
                }
            }

            delegate void [|D|](int a, int b, int c, int d, int e, int f);
            """;

        return test.RunAsync();
    }
}

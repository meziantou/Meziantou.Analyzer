using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseBannedSyntaxAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseBannedSyntaxAnalyzerTests
{
    // The rule reports the invalid entries with a second descriptor, so the markup must use the first one
    private static AnalyzerTest CreateTest(string bannedSyntaxes)
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", bannedSyntaxes));
        return test;
    }

    private static DiagnosticResult Diagnostic(int markupKey, string name, string message) =>
        new DiagnosticResult("MA0240", DiagnosticSeverity.Warning).WithLocation(markupKey).WithArguments(name, message);

    [Fact]
    public Task NoFile_NoDiagnostic()
    {
        var test = new AnalyzerTest
        {
            TestCode = """
                class Sample
                {
                    void M()
                    {
                        goto label;
                        label:
                        return;
                    }
                }
                """,
        };

        return test.RunAsync();
    }

    [Fact]
    public Task OtherFileName_NoDiagnostic()
    {
        var test = new AnalyzerTest();
        test.TestState.AdditionalFiles.Add(("BannedSymbols.txt", "GotoStatement"));
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
    public Task Kind_DefaultMessage()
    {
        var test = CreateTest("GotoStatement");
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
        test.ExpectedDiagnostics.Add(Diagnostic(0, "GotoStatement", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Kind_CustomMessage()
    {
        var test = CreateTest("GotoStatement; Use structured control flow instead");
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
        test.ExpectedDiagnostics.Add(Diagnostic(0, "GotoStatement", ": Use structured control flow instead"));

        return test.RunAsync();
    }

    [Fact]
    public Task KindWithDescendantAxis_Diagnostic()
    {
        var test = CreateTest("//GotoStatement");
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
    public Task MultipleLines_CommentsAndBlankLines()
    {
        var test = CreateTest("""
            # Control flow

            GotoStatement; Do not use goto
                LockStatement ; Use System.Threading.Lock
            """);
        test.TestCode = """
            class Sample
            {
                void M(object o)
                {
                    {|#0:lock (o) { }|}
                    {|#1:goto label;|}
                    label:
                    return;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "LockStatement", ": Use System.Threading.Lock"));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "GotoStatement", ": Do not use goto"));

        return test.RunAsync();
    }

    [Fact]
    public Task NestedNodes_Diagnostic()
    {
        var test = CreateTest("ConditionalExpression");
        test.TestCode = """
            class Sample
            {
                int M(bool a, bool b) => [|a ? [|b ? 1 : 2|] : 3|];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleFiles_Diagnostic()
    {
        var test = CreateTest("GotoStatement");
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.Shared.txt", "LockStatement"));
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
    public Task SameSyntaxBannedTwice_ReportedOncePerMessage()
    {
        var test = CreateTest("""
            GotoStatement; First
            //GotoStatement; First
            GotoStatement; Second
            """);
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
        test.ExpectedDiagnostics.Add(Diagnostic(0, "GotoStatement", ": First"));
        test.ExpectedDiagnostics.Add(Diagnostic(0, "GotoStatement", ": Second"));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_PrimaryConstructor_CustomMessage()
    {
        var test = CreateTest("//ClassDeclaration/ParameterList; Do not use primary constructors");
        test.TestCode = """
            class Sample{|#0:(int value)|}
            {
                Sample() : this(0) { }

                void M(int value) { }
            }

            class Other { }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "ParameterList", ": Do not use primary constructors"));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_Predicate_ReportsSelectedNode()
    {
        var test = CreateTest("//ClassDeclaration[ParameterList]");
        test.TestCode = """
            [|class Sample(int value)
            {
            }|]

            class Other { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_Union_Diagnostic()
    {
        var test = CreateTest("//ClassDeclaration/ParameterList | //StructDeclaration/ParameterList");
        test.TestCode = """
            class Sample[|(int value)|];

            struct S[|(int value)|];

            record R(int Value);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Query_NestedNodes_Diagnostic()
    {
        var test = CreateTest("//ConditionalExpression//ConditionalExpression");
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
        var test = CreateTest("//MethodDeclaration[@Identifier='Banned']");
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
        var test = CreateTest("//MethodDeclaration/@Modifiers[contains(., 'async')]");
        test.TestCode = """
            class Sample
            {
                {|#0:public async|} System.Threading.Tasks.Task A() => await System.Threading.Tasks.Task.Yield();
                public void B() { }
                void C() { }
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "MethodDeclaration/@Modifiers", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_SemicolonInStringLiteral_IsNotASeparator()
    {
        var test = CreateTest("""//StringLiteralExpression[@Token='";"']; Use a char""");
        test.TestCode = """
            class Sample
            {
                string A() => {|#0:";"|};
                string B() => ",";
            }
            """;
        test.ExpectedDiagnostics.Add(Diagnostic(0, "StringLiteralExpression", ": Use a char"));

        return test.RunAsync();
    }

    [Fact]
    public Task Query_ParentAxis_Diagnostic()
    {
        var test = CreateTest("//ReturnStatement/parent::Block/parent::MethodDeclaration/@Identifier");
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
        var test = CreateTest("//MethodDeclaration[preceding-sibling::FieldDeclaration and following-sibling::FieldDeclaration]/@Identifier");
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
    public Task Query_Count_Diagnostic()
    {
        var test = CreateTest("//*[count(ParameterList/Parameter) > 5]/@Identifier");
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

    [Fact]
    public Task Query_NoMatch_NoDiagnostic()
    {
        var test = CreateTest("//ClassDeclaration/ParameterList");
        test.TestCode = """
            class Sample
            {
                void M() { }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("GotoStatment")]
    [InlineData("//GotoStatment")]
    [InlineData("gotostatement")]
    [InlineData("None")]
    [InlineData("//ClassDeclaration[")]
    [InlineData("count(//ClassDeclaration)")]
    [InlineData("8743")]
    [InlineData("//ClassDeclaration[unknown-function()]")]
    public Task InvalidEntry_ReportedInFile(string query)
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", $$"""
            # Comment
            GotoStatement
            {|#0:{{query}}; message|}
            """));
        test.TestCode = """
            class Sample
            {
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0240", DiagnosticSeverity.Warning).WithLocation(0));

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidEntry_ValidEntriesAreApplied()
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            {|#0:UnknownKind|}
            GotoStatement
            """));
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#1:goto label;|}
                    label:
                    return;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0240", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("UnknownKind", "'UnknownKind' is not a member of SyntaxKind"));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "GotoStatement", ""));

        return test.RunAsync();
    }

    [Fact]
    public Task SameFileAddedTwice_ReportedOnce()
    {
        var test = new AnalyzerTest { MarkupOptions = MarkupOptions.UseFirstDescriptor };
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            {|#0:UnknownKind|}
            GotoStatement
            """));
        test.TestState.AdditionalFiles.Add(("BannedSyntaxes.txt", """
            UnknownKind
            GotoStatement
            """));
        test.TestCode = """
            class Sample
            {
                void M()
                {
                    {|#1:goto label;|}
                    label:
                    return;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0240", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("UnknownKind", "'UnknownKind' is not a member of SyntaxKind"));
        test.ExpectedDiagnostics.Add(Diagnostic(1, "GotoStatement", ""));

        return test.RunAsync();
    }
}

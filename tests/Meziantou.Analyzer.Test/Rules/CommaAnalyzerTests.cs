using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.CommaAnalyzer,
    Meziantou.Analyzer.Rules.CommaFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class CommaAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task OneLineDeclarationWithMissingTrailingComma_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public int A { get; set; }
                public int B { get; set; }

                public async System.Threading.Tasks.Task Test()
                {
                    new TypeName() { A = 1 };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleLinesDeclarationWithTrailingComma_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public int A { get; set; }
                public int B { get; set; }

                public async System.Threading.Tasks.Task Test()
                {
                    new TypeName()
                    {
                        A = 1,
                        B = 2,
                    };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleLinesDeclarationWithMissingTrailingComma_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public int A { get; set; }
                public int B { get; set; }

                public async System.Threading.Tasks.Task Test()
                {
                    new TypeName()
                    {
                        A = 1,
                        {|MA0007:B = 2|}
                    };
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public int A { get; set; }
                public int B { get; set; }

                public async System.Threading.Tasks.Task Test()
                {
                    new TypeName()
                    {
                        A = 1,
                        B = 2,
                    };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EnumsWithLeadingComma()
    {
        var test = CreateTest();
        test.TestCode = """
            enum TypeName
            {
                A = 1,
                B = 2,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EnumsWithoutLeadingComma()
    {
        var test = CreateTest();
        test.TestCode = """
            enum TypeName
            {
                A = 1,
                {|MA0007:B = 2|}
            }
            """;
        test.FixedCode = """
            enum TypeName
            {
                A = 1,
                B = 2,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AnonymousObjectWithLeadingComma()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = new
                    {
                        A = 1,
                        B = 2,
                    };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AnonymousObjectWithoutLeadingComma()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = new
                    {
                        A = 1,
                        {|MA0007:B = 2|}
                    };
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = new
                    {
                        A = 1,
                        B = 2,
                    };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitCtorWithoutLeadingComma()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public int A { get; set; }
                public int B { get; set; }

                public void Test()
                {
                    TypeName a = new()
                    {
                        A = 1,
                        {|MA0007:B = 2|}
                    };
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public int A { get; set; }
                public int B { get; set; }

                public void Test()
                {
                    TypeName a = new()
                    {
                        A = 1,
                        B = 2,
                    };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CollectionExpressionWithoutLeadingComma_ClosingBracketOnNextLine()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    int[] a =
                    [
                        1,
                        {|MA0007:2|}
                    ];
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    int[] a =
                    [
                        1,
                        2,
                    ];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CollectionExpressionWithoutLeadingComma_ClosingBracketOnSameLine()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test(int[] source)
                {
                    int[] a =
                    [
                        .. source];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CollectionExpressionWithoutLeadingComma_ClosingBracketOnSameLine_WithPreviousValue()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test(int[] source)
                {
                    int[] a =
                    [
                        1,
                        .. source];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CollectionExpressionWithoutLeadingComma_SpreadElementWithComment()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test(int[] source)
                {
                    int[] a =
                    [
                        1,
                        {|MA0007:.. source|} // comment
                    ];
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test(int[] source)
                {
                    int[] a =
                    [
                        1,
                        .. source, // comment
                    ];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SwitchExpressionWithoutLeadingComma_CatchAll()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = 0 switch
                    {
                        1 => 1,
                        {|MA0007:_ => 2|}
                    };
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = 0 switch
                    {
                        1 => 1,
                        _ => 2,
                    };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SwitchExpressionWithoutLeadingComma_IgnoreCatchAll()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0007.IgnoreCatchAllArm", "true");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = 0 switch
                    {
                        1 => 1,
                        _ => 2
                    };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SwitchExpressionWithoutLeadingComma()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = 0 switch
                    {
                        1 => 1,
                        {|MA0007:2 => 2|}
                    };
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = 0 switch
                    {
                        1 => 1,
                        2 => 2,
                    };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SwitchExpressionWithLeadingComma()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = 0 switch
                    {
                        1 => 1,
                        2 => 2,
                    };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithExpressionWithoutLeadingComma()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            var a = new Sample(1, 2);
            _ = a with
            {
                A = 3,
                {|MA0007:B = 4|}
            };

            record Sample(int A, int B);
            """;
        test.FixedCode = """
            var a = new Sample(1, 2);
            _ = a with
            {
                A = 3,
                B = 4,
            };

            record Sample(int A, int B);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithExpressionWithLeadingComma()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            var a = new Sample(1, 2);
            _ = a with
            {
                A = 3,
                B = 4,
            };

            record Sample(int A, int B);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithExpressionWithoutLeadingCommaSingleLine()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            var a = new Sample(1, 2);
            _ = a with { A = 3, B = 4 };

            record Sample(int A, int B);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyPatternWithoutTrailingComma()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public int A { get; set; }
                public int B { get; set; }

                public void Test()
                {
                    var obj = new TypeName();
                    _ = obj is
                    {
                        A: 1,
                        {|MA0007:B: 2|}
                    };
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public int A { get; set; }
                public int B { get; set; }

                public void Test()
                {
                    var obj = new TypeName();
                    _ = obj is
                    {
                        A: 1,
                        B: 2,
                    };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyPatternWithTrailingComma()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public int A { get; set; }
                public int B { get; set; }

                public void Test()
                {
                    var obj = new TypeName();
                    _ = obj is
                    {
                        A: 1,
                        B: 2,
                    };
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyPatternSingleLine()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public int A { get; set; }
                public int B { get; set; }

                public void Test()
                {
                    var obj = new TypeName();
                    _ = obj is { A: 1, B: 2 };
                }
            }
            """;

        return test.RunAsync();
    }
}

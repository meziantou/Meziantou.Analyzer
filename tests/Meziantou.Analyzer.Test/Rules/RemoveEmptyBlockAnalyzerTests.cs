using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.RemoveEmptyBlockAnalyzer,
    Meziantou.Analyzer.Rules.RemoveEmptyBlockFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RemoveEmptyBlockAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task EmptyElseBlock()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(bool condition)
                {
                    if (condition)
                    {
                    }
                    [|else
                    {
                    }|]
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyElseBlock_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(bool condition)
                {
                    if (condition)
                    {
                    }
                    [|else
                    {
                    }|]
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A(bool condition)
                {
                    if (condition)
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ElseBlockContainingABlock()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(bool condition)
                {
                    if (condition)
                    {
                    }
                    else
                    {
                        {
                        }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ElseBlockWithComment()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(bool condition)
                {
                    if (condition)
                    {
                    }
                    else
                    {
                        // Comment
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ElseBlockWithMultilineComment()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(bool condition)
                {
                    if (condition)
                    {
                    }
                    else
                    {
                        /*
                            Comment
                        */
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ElseBlockWithStatement()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(bool condition)
                {
                    if (condition)
                    {
                    }
                    else
                    {
                        _ = condition;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyFinallyBlock()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    try
                    {
                    }
                    [|finally
                    {
                    }|]
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyFinallyBlock_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    try
                    {
                    }
                    [|finally
                    {
                    }|]
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A()
                {
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyFinallyBlock_WithCatch_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    try
                    {
                    }
                    catch
                    {
                    }
                    [|finally
                    {
                    }|]
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A()
                {
                    try
                    {
                    }
                    catch
                    {
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FinallyBlockWithComment()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    try
                    {
                    }
                    finally
                    {
                        // Comment
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FinallyBlockWithMultilineComment()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    try
                    {
                    }
                    finally
                    {
                        /*
                            Comment
                        */
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FinallyBlockWithStatement()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(bool condition)
                {
                    try
                    {
                    }
                    finally
                    {
                        _ = condition;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyFinallyBlockInsideElseBlock_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void Foo() => throw null;
                void A(bool condition)
                {
                    if (condition)
                    {
                        Foo();
                    }
                    else
                    {
                        try
                        {
                            Foo();
                        }
                        [|finally
                        {
                        }|]
                    }
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                void Foo() => throw null;
                void A(bool condition)
                {
                    if (condition)
                    {
                        Foo();
                    }
                    else
                    {
                        {
                            Foo();
                        }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyElseBlockInsideFinallyBlock_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void Foo() => throw null;
                void A(bool condition)
                {
                    try
                    {
                        Foo();
                    }
                    finally
                    {
                        if (condition)
                        {
                            Foo();
                        }
                        [|else
                        {
                        }|]
                    }
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                void Foo() => throw null;
                void A(bool condition)
                {
                    try
                    {
                        Foo();
                    }
                    finally
                    {
                        if (condition)
                        {
                            Foo();
                        }
                    }
                }
            }
            """;

        return test.RunAsync();
    }
}

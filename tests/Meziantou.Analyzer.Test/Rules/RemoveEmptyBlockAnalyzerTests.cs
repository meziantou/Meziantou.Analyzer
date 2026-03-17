using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RemoveEmptyBlockAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<RemoveEmptyBlockAnalyzer>();
    }

    [Fact]
    public async Task EmptyElseBlock()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ElseBlockContainingABlock()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ElseBlockWithComment()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ElseBlockWithMultilineComment()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ElseBlockWithStatement()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    //

    [Fact]
    public async Task EmptyFinallyBlock()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FinallyBlockWithComment()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FinallyBlockWithMultilineComment()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FinallyBlockWithStatement()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}

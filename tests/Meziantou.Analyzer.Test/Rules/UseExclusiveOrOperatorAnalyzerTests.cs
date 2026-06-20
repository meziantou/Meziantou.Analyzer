using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseExclusiveOrOperatorAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseExclusiveOrOperatorAnalyzer>()
            .WithCodeFixProvider<UseExclusiveOrOperatorFixer>();
    }

    [Theory]
    [InlineData("(x && !y) || (!x && y)", "x ^ y")]
    [InlineData("(!x && y) || (x && !y)", "y ^ x")]
    [InlineData("(x && !y) || (y && !x)", "x ^ y")]
    [InlineData("(!y && x) || (!x && y)", "x ^ y")]
    public async Task UseExclusiveOrOperator(string expression, string fixedExpression)
    {
        var originalCode = $$"""
            class TestClass
            {
                void Test(bool x, bool y)
                {
                    _ = [|{{expression}}|];
                }
            }
            """;
        var fixedCode = $$"""
            class TestClass
            {
                void Test(bool x, bool y)
                {
                    _ = {{fixedExpression}};
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(fixedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UseExclusiveOrOperator_LocalVariables()
    {
        var originalCode = """
            class TestClass
            {
                void Test()
                {
                    var x = true;
                    var y = false;
                    _ = [|(x && !y) || (!x && y)|];
                }
            }
            """;
        var fixedCode = """
            class TestClass
            {
                void Test()
                {
                    var x = true;
                    var y = false;
                    _ = x ^ y;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(fixedCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("(x && y) || (!x && !y)")]
    [InlineData("(x && !y) || (x && y)")]
    [InlineData("(x || !y) || (!x && y)")]
    [InlineData("(x && !y) && (!x && y)")]
    [InlineData("x ^ y")]
    public async Task NoDiagnostic(string expression)
    {
        var originalCode = $$"""
            class TestClass
            {
                void Test(bool x, bool y)
                {
                    _ = {{expression}};
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_ForMemberAccess()
    {
        var originalCode = """
            class TestClass
            {
                bool X { get; }
                bool Y { get; }

                void Test()
                {
                    _ = (X && !Y) || (!X && Y);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }
}

using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseExclusiveOrOperatorAnalyzer,
    Meziantou.Analyzer.Rules.UseExclusiveOrOperatorFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseExclusiveOrOperatorAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Theory]
    [InlineData("(x && !y) || (!x && y)", "x ^ y")]
    [InlineData("(!x && y) || (x && !y)", "y ^ x")]
    [InlineData("(x && !y) || (y && !x)", "x ^ y")]
    [InlineData("(!y && x) || (!x && y)", "x ^ y")]
    public Task UseExclusiveOrOperator(string expression, string fixedExpression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                void Test(bool x, bool y)
                {
                    _ = [|{{expression}}|];
                }
            }
            """;
        test.FixedCode = $$"""
            class TestClass
            {
                void Test(bool x, bool y)
                {
                    _ = {{fixedExpression}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UseExclusiveOrOperator_LocalVariables()
    {
        var test = CreateTest();
        test.TestCode = """
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
        test.FixedCode = """
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

        return test.RunAsync();
    }

    [Theory]
    [InlineData("(x && y) || (!x && !y)")]
    [InlineData("(x && !y) || (x && y)")]
    [InlineData("(x || !y) || (!x && y)")]
    [InlineData("(x && !y) && (!x && y)")]
    [InlineData("x ^ y")]
    public Task NoDiagnostic(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                void Test(bool x, bool y)
                {
                    _ = {{expression}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_ForMemberAccess()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }
}

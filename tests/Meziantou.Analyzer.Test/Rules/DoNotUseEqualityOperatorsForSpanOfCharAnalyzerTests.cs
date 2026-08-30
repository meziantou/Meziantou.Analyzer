using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.DoNotUseEqualityOperatorsForSpanOfCharAnalyzer,
    Meziantou.Analyzer.Rules.DoNotUseEqualityOperatorsForSpanOfCharFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseEqualityOperatorsForSpanOfCharAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task SpanEquals()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                void A()
                {
                    _ = [|"a".AsSpan() == "ab".AsSpan().Slice(0, 1)|];
                }
            }
            """;
        test.FixedCode = """
            using System;
            class Test
            {
                void A()
                {
                    _ = "a".AsSpan().SequenceEqual("ab".AsSpan().Slice(0, 1));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SpanNotEquals()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                void A()
                {
                    _ = [|"a".AsSpan() != "ab".AsSpan().Slice(0, 1)|];
                }
            }
            """;
        test.FixedCode = """
            using System;
            class Test
            {
                void A()
                {
                    _ = !"a".AsSpan().SequenceEqual("ab".AsSpan().Slice(0, 1));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringEquals()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                void A()
                {
                    _ = "a" == "ab";
                }
            }
            """;

        return test.RunAsync();
    }
}

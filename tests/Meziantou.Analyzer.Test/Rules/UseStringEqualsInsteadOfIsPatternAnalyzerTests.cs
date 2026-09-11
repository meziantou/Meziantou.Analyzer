using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseStringEqualsInsteadOfIsPatternAnalyzer,
    Meziantou.Analyzer.Rules.UseStringEqualsInsteadOfIsPatternFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringEqualsInsteadOfIsPatternAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task IsStringEmpty()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test(string str)
                {
                    _ = str is "";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsNull()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test(string str)
                {
                    _ = str is null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsNotNull()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test(string str)
                {
                    _ = str is not null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test(string str)
                {
                    _ = str is {|MA0127:"b"|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching_CodeFix_Ordinal()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test(string str)
                {
                    _ = str is {|MA0127:"b"|};
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test(string str)
                {
                    _ = string.Equals(str, "b", System.StringComparison.Ordinal);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching_CodeFix_OrdinalIgnoreCase()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test(string str)
                {
                    _ = str is {|MA0127:"b"|};
                }
            }
            """;
        test.CodeActionIndex = 1;
        test.FixedCode = """
            class TypeName
            {
                public void Test(string str)
                {
                    _ = string.Equals(str, "b", System.StringComparison.OrdinalIgnoreCase);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching_Object_CodeFix_Ordinal()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public bool Test(object value) => value is {|MA0127:"hello"|};
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public bool Test(object value) => string.Equals(value as string, "hello", System.StringComparison.Ordinal);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching_Object_CodeFix_OrdinalIgnoreCase()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public bool Test(object value) => value is {|MA0127:"hello"|};
            }
            """;
        test.CodeActionIndex = 1;
        test.FixedCode = """
            class TypeName
            {
                public bool Test(object value) => string.Equals(value as string, "hello", System.StringComparison.OrdinalIgnoreCase);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching_ObjectExpression_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public bool Test(object a, object b) => (a ?? b) is {|MA0127:"hello"|};
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public bool Test(object a, object b) => string.Equals((a ?? b) as string, "hello", System.StringComparison.Ordinal);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching_Dynamic_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public bool Test(dynamic value) => value is {|MA0127:"hello"|};
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public bool Test(dynamic value) => string.Equals(value as string, "hello", System.StringComparison.Ordinal);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching_Interface_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public bool Test(System.IComparable value) => value is {|MA0127:"hello"|};
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public bool Test(System.IComparable value) => string.Equals(value as string, "hello", System.StringComparison.Ordinal);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching_TypeParameter_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public bool Test<T>(T value) => value is {|MA0127:"hello"|};
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public bool Test<T>(T value) => string.Equals(value as string, "hello", System.StringComparison.Ordinal);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching_ReadOnlySpan_CodeFix_Ordinal()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public bool Test(System.ReadOnlySpan<char> value) => value is {|MA0127:"hello"|};
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public bool Test(System.ReadOnlySpan<char> value) => System.MemoryExtensions.Equals(value, "hello", System.StringComparison.Ordinal);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching_ReadOnlySpan_CodeFix_OrdinalIgnoreCase()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public bool Test(System.ReadOnlySpan<char> value) => value is {|MA0127:"hello"|};
            }
            """;
        test.CodeActionIndex = 1;
        test.FixedCode = """
            class TypeName
            {
                public bool Test(System.ReadOnlySpan<char> value) => System.MemoryExtensions.Equals(value, "hello", System.StringComparison.OrdinalIgnoreCase);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching_Span_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public bool Test(System.Span<char> value) => value is {|MA0127:"hello"|};
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public bool Test(System.Span<char> value) => System.MemoryExtensions.Equals(value, "hello", System.StringComparison.Ordinal);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching_Complex1()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                string Value { get; set; }

                public void Test(TypeName obj)
                {
                    _ = obj is { Value: {|MA0127:"b"|}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching_Complex2()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                string Value { get; set; }

                public void Test(TypeName obj)
                {
                    _ = obj is { Value: {|MA0127:"b"|} or {|MA0127:"c"|}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PatternMatching_Complex3()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                string Value { get; set; }

                public void Test(TypeName obj)
                {
                    _ = obj is { Value: var a and ({|MA0127:"b"|} or {|MA0127:"c"|})};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public void Rule_SeverityAndDefault()
    {
        var rule = new UseStringEqualsInsteadOfIsPatternAnalyzer().SupportedDiagnostics[0];
        Assert.Equal(DiagnosticSeverity.Hidden, rule.DefaultSeverity);
        Assert.True(rule.IsEnabledByDefault);
    }
}

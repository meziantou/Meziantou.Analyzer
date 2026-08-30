using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.ReplaceEnumToStringWithNameofAnalyzer,
    Meziantou.Analyzer.Rules.ReplaceEnumToStringWithNameofFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ReplaceEnumToStringWithNameofAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task ConstantEnumValueToString()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    _ = [|MyEnum.A.ToString()|];
                }
            }

            enum MyEnum
            {
                A,
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A()
                {
                    _ = nameof(MyEnum.A);
                }
            }

            enum MyEnum
            {
                A,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EnumVariableToString()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    var a = MyEnum.A;
                    _ = a.ToString();
                }
            }

            enum MyEnum
            {
                A,
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"G\"")]
    [InlineData("\"g\"")]
    [InlineData("\"F\"")]
    [InlineData("\"f\"")]
    public Task ToString_Formats(string format)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class Test
            {
                void A()
                {
                    _ = [|MyEnum.A.ToString(format: {{format}})|];
                }
            }

            enum MyEnum
            {
                A,
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A()
                {
                    _ = nameof(MyEnum.A);
                }
            }

            enum MyEnum
            {
                A,
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("\"x\"")]
    [InlineData("\"X\"")]
    [InlineData("\"d\"")]
    [InlineData("\"D\"")]
    public Task ToString_IncompatibleFormats(string format)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class Test
            {
                void A()
                {
                    _ = MyEnum.A.ToString(format: {{format}});
                }
            }

            enum MyEnum
            {
                A,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ToString_DynamicFormat()
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class Test
            {
                void A(string format)
                {
                    _ = MyEnum.A.ToString(format);
                }
            }

            enum MyEnum
            {
                A,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedString()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    _ = $"[|{MyEnum.A}|]";
                    _ = $"[|{MyEnum.A:g}|]";
                    _ = $"[|{MyEnum.A:G}|]";
                    _ = $"[|{MyEnum.A:f}|]";
                    _ = $"{MyEnum.A:D}";
                    _ = $"{MyEnum.A:x}";
                }
            }

            enum MyEnum
            {
                A,
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A()
                {
                    _ = $"{nameof(MyEnum.A)}";
                    _ = $"{nameof(MyEnum.A)}";
                    _ = $"{nameof(MyEnum.A)}";
                    _ = $"{nameof(MyEnum.A)}";
                    _ = $"{MyEnum.A:D}";
                    _ = $"{MyEnum.A:x}";
                }
            }

            enum MyEnum
            {
                A,
            }
            """;

        return test.RunAsync();
    }
}

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
                    _ = {|MA0052:MyEnum.A.ToString()|};
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
    public Task EnumMemberWithAlias_UseFirstDeclaredMember()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    _ = {|MA0052:MyEnum.B.ToString()|};
                    _ = {|MA0052:MyEnum.D.ToString("G")|};
                    _ = $"{|MA0052:{MyEnum.B}|}";
                }
            }

            enum MyEnum
            {
                A = 0,
                B = 0,
                C = 1,
                D = C,
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A()
                {
                    _ = nameof(MyEnum.A);
                    _ = nameof(MyEnum.C);
                    _ = $"{nameof(MyEnum.A)}";
                }
            }

            enum MyEnum
            {
                A = 0,
                B = 0,
                C = 1,
                D = C,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EnumMemberWithAlias_UseOtherMember()
    {
        var test = CreateTest();
        test.CodeActionIndex = 1;
        test.CodeActionEquivalenceKey = "Use nameof(MyEnum.B)";
        test.TestCode = """
            class Test
            {
                void A()
                {
                    _ = {|MA0052:MyEnum.A.ToString()|};
                }
            }

            enum MyEnum
            {
                A = 0,
                B = 0,
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A()
                {
                    _ = nameof(MyEnum.B);
                }
            }

            enum MyEnum
            {
                A = 0,
                B = 0,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EnumMemberWithAlias_UsingStatic()
    {
        var test = CreateTest();
        test.TestCode = """
            using static MyEnum;

            class Test
            {
                void M()
                {
                    _ = {|MA0052:B.ToString()|};
                }
            }

            enum MyEnum
            {
                A = 0,
                B = 0,
            }
            """;
        test.FixedCode = """
            using static MyEnum;

            class Test
            {
                void M()
                {
                    _ = nameof(A);
                }
            }

            enum MyEnum
            {
                A = 0,
                B = 0,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EnumMemberWithAlias_KeywordName()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    _ = {|MA0052:MyEnum.B.ToString()|};
                }
            }

            enum MyEnum
            {
                @class = 0,
                B = 0,
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A()
                {
                    _ = nameof(MyEnum.@class);
                }
            }

            enum MyEnum
            {
                @class = 0,
                B = 0,
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
                    _ = {|MA0052:MyEnum.A.ToString(format: {{format}})|};
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
                    _ = $"{|MA0052:{MyEnum.A}|}";
                    _ = $"{|MA0052:{MyEnum.A:g}|}";
                    _ = $"{|MA0052:{MyEnum.A:G}|}";
                    _ = $"{|MA0052:{MyEnum.A:f}|}";
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

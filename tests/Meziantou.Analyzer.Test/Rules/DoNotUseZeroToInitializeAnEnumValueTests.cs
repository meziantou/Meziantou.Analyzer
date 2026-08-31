using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.DoNotUseZeroToInitializeAnEnumValue,
    Meziantou.Analyzer.Rules.DoNotUseZeroToInitializeAnEnumValueFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseZeroToInitializeAnEnumValueTests
{
    private static CodeFixTest CreateTest() => new();

    public static TheoryData<string, string> GetCombinationZero()
    {
        var result = new TheoryData<string, string>();
        var values = new[]
        {
            "0",
            "0u",
            "0L",
            "0uL",
            "0b0",
            "0x0",
            "0f",
            "0d",
            "0m",
            "(byte)0",
            "(sbyte)0",
            "(int)0",
            "(uint)0",
            "(float)0",
        };

        foreach (var type in new[] { "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong" })
        {
            foreach (var value in values)
            {
                result.Add(type, value);
            }
        }

        return result;
    }

    public static TheoryData<string, string> GetCombinationNonZero()
    {
        var result = new TheoryData<string, string>();
        var values = new[]
        {
            "1",
            "1u",
            "1L",
            "1uL",
            "0b1",
            "0x1",
            "1d",
            "1m",
            "(byte)1",
            "(sbyte)1",
            "(int)1",
            "(uint)1",
        };

        foreach (var type in new[] { "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong" })
        {
            foreach (var value in values)
            {
                result.Add(type, value);
            }
        }

        return result;
    }

    [Theory]
    [MemberData(nameof(GetCombinationZero))]
    public Task EnumBaseType_Zero(string baseType, string value)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            enum MyEnum : {{baseType}} { A = 0, B = 1 }

            class Test
            {
                void A()
                {
                    MyEnum a = [|{{value}}|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(GetCombinationNonZero))]
    public Task EnumBaseType_NonZero(string baseType, string value)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            enum MyEnum : {{baseType}} { A = 0, B = 1 }

            class Test
            {
                void A()
                {
                    MyEnum a = (MyEnum){{value}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Assignation_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void A()
                {
                    MyEnum a = MyEnum.A;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Assignation_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void A()
                {
                    MyEnum a = [|0|];
                }
            }
            """;
        test.FixedCode = """
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void A()
                {
                    MyEnum a = MyEnum.A;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Assignation_CodeFix_NoNamedZero()
    {
        var test = CreateTest();
        test.TestCode = """
            enum MyEnum { A = 1, B = 2 }

            class Test
            {
                void A()
                {
                    MyEnum a = [|0|];
                }
            }
            """;
        test.FixedCode = """
            enum MyEnum { A = 1, B = 2 }

            class Test
            {
                void A()
                {
                    MyEnum a = (MyEnum)0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Reassignation()
    {
        var test = CreateTest();
        test.TestCode = """
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void A()
                {
                    MyEnum a = default;
                    a = [|0|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Assignation_ExplicitCast()
    {
        var test = CreateTest();
        test.TestCode = """
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void A()
                {
                    MyEnum b = (MyEnum)0;
                    b = (MyEnum)0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Assignation_EnumValue_Zero()
    {
        var test = CreateTest();
        test.TestCode = """
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void A()
                {
                    MyEnum c = MyEnum.A;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Assignation_EnumValue_NonZero()
    {
        var test = CreateTest();
        test.TestCode = """
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void A()
                {
                    MyEnum d = MyEnum.B;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Assignation_Default()
    {
        var test = CreateTest();
        test.TestCode = """
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void A()
                {
                    MyEnum e = default;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Assignation_NonEnumType()
    {
        var test = CreateTest();
        test.TestCode = """
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void A()
                {
                    long f = 0;
                    long g = (long)0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("MyEnum.A")]
    [InlineData("MyEnum.B")]
    [InlineData("(MyEnum)0")]
    [InlineData("(MyEnum)0u")]
    [InlineData("a")]
    public Task MethodInvocation(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void A(MyEnum a)
                {
                    A({{code}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0u")]
    public Task MethodInvocation_Diagnostic(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void A(MyEnum a)
                {
                    A([|{{code}}|]);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("MyEnum.A")]
    [InlineData("(MyEnum)0")]
    public Task OptionalParameter(string defaultValue)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            enum MyEnum { A = 0, B = 1 }
            class Test
            {
                void A(MyEnum a = {{defaultValue}})
                {
                    A();
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0u")]
    [InlineData("0b0")]
    [InlineData("0x0")]
    [InlineData("0f")]
    [InlineData("0d")]
    [InlineData("0m")]
    [InlineData("0L")]
    [InlineData("0uL")]
    [InlineData("(byte)0")]
    [InlineData("(sbyte)0")]
    [InlineData("(int)0")]
    [InlineData("(uint)0")]
    [InlineData("(float)0")]
    public Task OptionalParameter_Diagnostic(string defaultValue)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            enum MyEnum { A = 0, B = 1 }
            class Test
            {
                void A(MyEnum a = [|{{defaultValue}}|])
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitOptionalParameter()
    {
        var test = CreateTest();
        test.TestCode = $$"""
            enum MyEnum { A = 0, B = 1 }
            class Test
            {
                void A(MyEnum a = [|0|])
                {
                    A(); // ok
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitOptionalParameter_NonZero()
    {
        var test = CreateTest();
        test.TestCode = $$"""
            enum MyEnum { A = 0, B = 1 }
            class Test
            {
                void A(MyEnum a = MyEnum.B)
                {
                    A();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/525")]
    public Task ImplicitParameterInAttribute()
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;

            public enum MyEnum
            {
                None = 0,
                Some = 1,
            }

            [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
            public class MyAttribute : Attribute
            {
                public MyAttribute(MyEnum bar = MyEnum.None) { }
            }

            [MyAttribute]
            [MyAttribute(MyEnum.None)]
            [MyAttribute(MyEnum.Some)]
            [MyAttribute([|0|])]
            public class MyClass
            {
                public MyClass(MyEnum foo = MyEnum.None) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1210")]
    public Task ExcludeEnumWithoutZeroMember_NoZeroMember_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0099.exclude_enum_without_zero_member", "true");
        test.TestCode = """
            enum MyEnum { A = 1, B = 2 }

            class Test
            {
                void M()
                {
                    MyEnum a = 0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1210")]
    public Task ExcludeEnumWithoutZeroMember_HasZeroMember_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0099.exclude_enum_without_zero_member", "true");
        test.TestCode = """
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void M()
                {
                    MyEnum a = [|0|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1210")]
    public Task ReportOnArgument_ZeroInArgument_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0099.report_on", "argument");
        test.TestCode = """
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void M(MyEnum x) { }

                void A()
                {
                    M([|0|]);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1210")]
    public Task ReportOnArgument_ZeroInAssignment_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0099.report_on", "argument");
        test.TestCode = """
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void M()
                {
                    MyEnum a = 0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1210")]
    public Task ReportOnArgument_ZeroInOptionalParameterDefault_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0099.report_on", "argument");
        test.TestCode = """
            enum MyEnum { A = 0, B = 1 }

            class Test
            {
                void M(MyEnum x = 0)
                {
                    M();
                }
            }
            """;

        return test.RunAsync();
    }
}

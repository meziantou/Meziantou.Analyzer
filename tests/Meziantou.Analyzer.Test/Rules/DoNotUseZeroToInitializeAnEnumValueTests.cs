using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public class DoNotUseZeroToInitializeAnEnumValueTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotUseZeroToInitializeAnEnumValue>();
    }

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
    public async Task EnumBaseType_Zero(string baseType, string value)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
enum MyEnum : {{baseType}} { A = 0, B = 1 }

class Test
{
    void A()
    {
        MyEnum a = [|{{value}}|];
    }
}
""")
              .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(GetCombinationNonZero))]
    public async Task EnumBaseType_NonZero(string baseType, string value)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
enum MyEnum : {{baseType}} { A = 0, B = 1 }

class Test
{
    void A()
    {
        MyEnum a = (MyEnum){{value}};
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task Assignation_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
enum MyEnum { A = 0, B = 1 }

class Test
{
    void A()
    {
        MyEnum a = MyEnum.A;
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task Reassignation()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
enum MyEnum { A = 0, B = 1 }

class Test
{
    void A()
    {
        MyEnum a = default;
        a = [|0|];
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task Assignation_ExplicitCast()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
enum MyEnum { A = 0, B = 1 }

class Test
{
    void A()
    {
        MyEnum b = (MyEnum)0;
        b = (MyEnum)0;
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task Assignation_EnumValue_Zero()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
enum MyEnum { A = 0, B = 1 }

class Test
{
    void A()
    {
        MyEnum c = MyEnum.A;
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task Assignation_EnumValue_NonZero()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
enum MyEnum { A = 0, B = 1 }

class Test
{
    void A()
    {
        MyEnum d = MyEnum.B;
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task Assignation_Default()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
enum MyEnum { A = 0, B = 1 }

class Test
{
    void A()
    {
        MyEnum e = default;
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task Assignation_NonEnumType()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
enum MyEnum { A = 0, B = 1 }

class Test
{
    void A()
    {
        long f = 0;
        long g = (long)0;
    }
}
")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("MyEnum.A")]
    [InlineData("MyEnum.B")]
    [InlineData("(MyEnum)0")]
    [InlineData("(MyEnum)0u")]
    [InlineData("a")]
    public async Task MethodInvocation(string code)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
                    enum MyEnum { A = 0, B = 1 }

                    class Test
                    {
                        void A(MyEnum a)
                        {
                            A({{code}});
                        }
                    }
                    """)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0u")]
    public async Task MethodInvocation_Diagnostic(string code)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
                    enum MyEnum { A = 0, B = 1 }

                    class Test
                    {
                        void A(MyEnum a)
                        {
                            A([|{{code}}|]);
                        }
                    }
                    """)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("MyEnum.A")]
    [InlineData("(MyEnum)0")]
    public async Task OptionalParameter(string defaultValue)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
enum MyEnum { A = 0, B = 1 }
class Test
{
    void A(MyEnum a = {{defaultValue}})
    {
        A();
    }
}
""")
              .ValidateAsync();
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
    public async Task OptionalParameter_Diagnostic(string defaultValue)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
enum MyEnum { A = 0, B = 1 }
class Test
{
    void A(MyEnum a = [|{{defaultValue}}|])
    {
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task ImplicitOptionalParameter()
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
enum MyEnum { A = 0, B = 1 }
class Test
{
    void A(MyEnum a = [|0|])
    {
        A(); // ok
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task ImplicitOptionalParameter_NonZero()
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
enum MyEnum { A = 0, B = 1 }
class Test
{
    void A(MyEnum a = MyEnum.B)
    {
        A();
    }
}
""")
              .ValidateAsync();
    }
}

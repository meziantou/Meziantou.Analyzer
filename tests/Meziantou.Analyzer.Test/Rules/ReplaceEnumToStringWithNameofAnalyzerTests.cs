using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ReplaceEnumToStringWithNameofAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<ReplaceEnumToStringWithNameofAnalyzer>()
            .WithCodeFixProvider<ReplaceEnumToStringWithNameofFixer>();
    }

    [Fact]
    public async Task ConstantEnumValueToString()
    {
        const string SourceCode = @"
class Test
{
    void A()
    {
        _ = [||]MyEnum.A.ToString();
    }
}

enum MyEnum
{
    A,
}";

        const string CodeFix = @"
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
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task EnumVariableToString()
    {
        const string SourceCode = @"
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
}";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"G\"")]
    [InlineData("\"g\"")]
    [InlineData("\"F\"")]
    [InlineData("\"f\"")]
    public async Task ToString_Formats(string format)
    {
        var sourceCode = $$"""
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

        var fix = """
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

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(fix)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("\"x\"")]
    [InlineData("\"X\"")]
    [InlineData("\"d\"")]
    [InlineData("\"D\"")]
    public async Task ToString_IncompatibleFormats(string format)
    {
        var sourceCode = $$"""
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

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ToString_DynamicFormat()
    {
        var sourceCode = $$"""
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

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
}

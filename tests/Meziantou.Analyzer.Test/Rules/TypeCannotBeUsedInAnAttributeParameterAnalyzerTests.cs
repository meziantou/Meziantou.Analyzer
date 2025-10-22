using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class TypeCannotBeUsedInAnAttributeParameterAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.Net9_0)
            .WithAnalyzer<TypeCannotBeUsedInAnAttributeParameterAnalyzer>();
    }

    [Fact]
    public async Task Ctor_NoParameter()
    {
        var sourceCode = """
            [Sample()]
            public class SampleAttribute : System.Attribute
            {
                public SampleAttribute() { }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("object")]
    [InlineData("System.Type")]
    [InlineData("byte")]
    [InlineData("sbyte")]
    [InlineData("short")]
    [InlineData("ushort")]
    [InlineData("int")]
    [InlineData("uint")]
    [InlineData("long")]
    [InlineData("ulong")]
    [InlineData("float")]
    [InlineData("double")]
    [InlineData("char")]
    [InlineData("string")]
    [InlineData("System.DayOfWeek")]
    [InlineData("System.DayOfWeek[]")]
    public async Task Ctor_Valid(string type)
    {
        var sourceCode = $$"""
            [Sample(default)]
            public class SampleAttribute : System.Attribute
            {
                public SampleAttribute({{type}} a) { }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("System.Action")]
    [InlineData("System.DayOfWeek[,]")]
    public async Task Ctor_Invalid(string type)
    {
        var sourceCode = $$"""
            public class SampleAttribute : System.Attribute
            {
                public SampleAttribute({{type}} [|a|]) { }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Property_Internal()
    {
        var sourceCode = """
            public class SampleAttribute : System.Attribute
            {
                internal System.Action A { get; set; }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Property_Valid()
    {
        var sourceCode = """
            public class SampleAttribute : System.Attribute
            {
                public int A { get; set; }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Property_Invalid()
    {
        var sourceCode = """
            public class SampleAttribute : System.Attribute
            {
                public System.Action [|A|] { get; set; }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Property_Private()
    {
        var sourceCode = """
            public class SampleAttribute : System.Attribute
            {
                private System.Action A { get; set; }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Property_Static()
    {
        var sourceCode = """
            public class SampleAttribute : System.Attribute
            {
                public static System.Action A { get; set; }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Property_GetOnly()
    {
        var sourceCode = """
            public class SampleAttribute : System.Attribute
            {
                public System.Action A { get; }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Property_Init()
    {
        var sourceCode = """
            public class SampleAttribute : System.Attribute
            {
                public System.Action [|A|] { get; init; }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Field_Internal()
    {
        var sourceCode = """
            public class SampleAttribute : System.Attribute
            {
                internal System.Action A;
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Field_Valid()
    {
        var sourceCode = """
            public class SampleAttribute : System.Attribute
            {
                public int A;
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Field_Invalid()
    {
        var sourceCode = """
            public class SampleAttribute : System.Attribute
            {
                public System.Action [|A|];
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Field_Private()
    {
        var sourceCode = """
            public class SampleAttribute : System.Attribute
            {
                private System.Action A;
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Field_Static()
    {
        var sourceCode = """
            public class SampleAttribute : System.Attribute
            {
                public static System.Action A;
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Field_Const()
    {
        var sourceCode = """
            public class SampleAttribute : System.Attribute
            {
                public const int A = 1;
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
}

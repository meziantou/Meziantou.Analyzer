using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.TypeCannotBeUsedInAnAttributeParameterAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class TypeCannotBeUsedInAnAttributeParameterAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task Ctor_NoParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            [Sample()]
            public class SampleAttribute : System.Attribute
            {
                public SampleAttribute() { }
            }
            """;

        return test.RunAsync();
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
    public Task Ctor_Valid(string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            [Sample(default)]
            public class SampleAttribute : System.Attribute
            {
                public SampleAttribute({{type}} a) { }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("System.Action")]
    [InlineData("System.DayOfWeek[,]")]
    public Task Ctor_Invalid(string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            public class SampleAttribute : System.Attribute
            {
                public SampleAttribute({{type}} {|MA0170:a|}) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_Internal()
    {
        var test = CreateTest();
        test.TestCode = """
            public class SampleAttribute : System.Attribute
            {
                internal System.Action A { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_Valid()
    {
        var test = CreateTest();
        test.TestCode = """
            public class SampleAttribute : System.Attribute
            {
                public int A { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_Invalid()
    {
        var test = CreateTest();
        test.TestCode = """
            public class SampleAttribute : System.Attribute
            {
                public System.Action {|MA0170:A|} { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_Private()
    {
        var test = CreateTest();
        test.TestCode = """
            public class SampleAttribute : System.Attribute
            {
                private System.Action A { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_Static()
    {
        var test = CreateTest();
        test.TestCode = """
            public class SampleAttribute : System.Attribute
            {
                public static System.Action A { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_GetOnly()
    {
        var test = CreateTest();
        test.TestCode = """
            public class SampleAttribute : System.Attribute
            {
                public System.Action A { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_Init()
    {
        var test = CreateTest();
        test.TestCode = """
            public class SampleAttribute : System.Attribute
            {
                public System.Action {|MA0170:A|} { get; init; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_Internal()
    {
        var test = CreateTest();
        test.TestCode = """
            public class SampleAttribute : System.Attribute
            {
                internal System.Action A;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_Valid()
    {
        var test = CreateTest();
        test.TestCode = """
            public class SampleAttribute : System.Attribute
            {
                public int A;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_Invalid()
    {
        var test = CreateTest();
        test.TestCode = """
            public class SampleAttribute : System.Attribute
            {
                public System.Action {|MA0170:A|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_Private()
    {
        var test = CreateTest();
        test.TestCode = """
            public class SampleAttribute : System.Attribute
            {
                private System.Action A;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_Static()
    {
        var test = CreateTest();
        test.TestCode = """
            public class SampleAttribute : System.Attribute
            {
                public static System.Action A;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_Const()
    {
        var test = CreateTest();
        test.TestCode = """
            public class SampleAttribute : System.Attribute
            {
                public const int A = 1;
            }
            """;

        return test.RunAsync();
    }
}

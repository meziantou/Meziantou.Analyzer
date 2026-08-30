using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.AttributeNameShouldEndWithAttributeAnalyzer,
    Meziantou.Analyzer.Rules.TypeNameShouldEndWithSuffixFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AttributeNameShouldEndWithAttributeAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task NameEndsWithAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            class CustomAttribute : System.Attribute
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NameDoesNotEndWithAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            class [|CustomAttr|] : System.Attribute
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NameDoesNotEndWithAttribute_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class [|CustomAttr|] : System.Attribute
            {
            }
            """;
        test.FixedCode = """
            class CustomAttrAttribute : System.Attribute
            {
            }
            """;

        return test.RunAsync();
    }
}

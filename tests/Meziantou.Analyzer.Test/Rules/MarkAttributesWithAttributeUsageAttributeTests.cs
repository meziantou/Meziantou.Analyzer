using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MarkAttributesWithAttributeUsageAttributeTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<MarkAttributesWithAttributeUsageAttributeAnalyzer>()
            .WithCodeFixProvider<MarkAttributesWithAttributeUsageAttributeFixer>();
    }

    [Fact]
    public async Task ClassInheritsFromAttribute_MissingAttribute_ShouldReportError()
    {
        const string SourceCode = "class [||]TestAttribute : System.Attribute { }";

        const string CodeFix = """
            [System.AttributeUsage(System.AttributeTargets.All)]
            class TestAttribute : System.Attribute { }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ClassDoesNotInheritsFromAttribute_ShouldNotReportError()
    {
        await CreateProjectBuilder()
              .WithSourceCode("class TestAttribute : System.Object { }")
              .ValidateAsync();
    }

    [Fact]
    public async Task ClassHasAttribute_ShouldNotReportError()
    {
        const string SourceCode = """
            [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = false, Inherited = true)]
            class TestAttribute : System.Attribute { }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AbstractClass_ShouldNotReportError()
    {
        const string SourceCode = """
            abstract class TestAttribute : System.Attribute { }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ParentClassHasAttribute_ShouldNotReportError()
    {
        const string SourceCode = """
            [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = false, Inherited = true)]
            class TestAttribute : System.Attribute { }
            class ChildTestAttribute : TestAttribute { }
            class GrandChildTestAttribute : ChildTestAttribute { }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}

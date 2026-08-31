using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.MarkAttributesWithAttributeUsageAttributeAnalyzer,
    Meziantou.Analyzer.Rules.MarkAttributesWithAttributeUsageAttributeFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MarkAttributesWithAttributeUsageAttributeTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task ClassInheritsFromAttribute_MissingAttribute_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = "class {|MA0010:TestAttribute|} : System.Attribute { }";
        test.FixedCode = """
            [System.AttributeUsage(System.AttributeTargets.All)]
            class TestAttribute : System.Attribute { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClassDoesNotInheritsFromAttribute_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = "class TestAttribute : System.Object { }";

        return test.RunAsync();
    }

    [Fact]
    public Task ClassHasAttribute_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = false, Inherited = true)]
            class TestAttribute : System.Attribute { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AbstractClass_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            abstract class TestAttribute : System.Attribute { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParentClassHasAttribute_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = false, Inherited = true)]
            class TestAttribute : System.Attribute { }
            class ChildTestAttribute : TestAttribute { }
            class GrandChildTestAttribute : ChildTestAttribute { }
            """;

        return test.RunAsync();
    }
}

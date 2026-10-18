using Microsoft.CodeAnalysis.Testing;
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

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1682")]
    public Task InvalidCode_ClassWithoutName_NoCodeFix()
    {
        const string Source = """
            class {|MA0010:|}: System.Attribute { }
            """;
        var test = CreateTest();
        // The code does not compile, so the compiler diagnostics cannot be verified
        test.CompilerDiagnostics = CompilerDiagnostics.None;
        test.TestCode = Source;
        test.FixedCode = Source;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidCode_RecordInheritsFromAttribute_MissingAttribute_ShouldReportError()
    {
        var test = CreateTest();
        // A record cannot derive from Attribute (CS8864), so the compiler diagnostics cannot be verified
        test.CompilerDiagnostics = CompilerDiagnostics.None;
        test.TestCode = "record {|MA0010:TestAttribute|} : System.Attribute { }";
        test.FixedCode = """
            [System.AttributeUsage(System.AttributeTargets.All)]
            record TestAttribute : System.Attribute { }
            """;

        return test.RunAsync();
    }
}

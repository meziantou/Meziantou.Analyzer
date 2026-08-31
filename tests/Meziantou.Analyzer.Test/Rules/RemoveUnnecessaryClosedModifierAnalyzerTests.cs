#if ROSLYN_5_9_OR_GREATER
using Microsoft.CodeAnalysis.CSharp;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.RemoveUnnecessaryClosedModifierAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RemoveUnnecessaryClosedModifierAnalyzerTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.LanguageVersion = LanguageVersion.Preview;
        return test;
    }

    [Fact]
    public Task ClosedClass_WithoutDerivedType_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            {|MA0216:closed|} class Sample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedClass_WithDerivedType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            closed class Sample
            {
            }

            class Derived : Sample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedClass_WithDerivedTypeInAnotherFile_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = "closed class Sample;";
        test.TestState.Sources.Add("class Derived : Sample;");

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedRecord_WithoutDerivedType_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            {|MA0216:closed|} record Sample;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedRecord_WithDerivedType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            closed record Sample;
            record Derived : Sample;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonClosedClass_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedClass_WithOtherModifiers_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public {|MA0216:closed|} partial class Sample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedPartialClass_WithoutDerivedType_ReportsDiagnosticOnDeclarationWithModifier()
    {
        var test = CreateTest();
        test.TestCode = """
            {|MA0216:closed|} partial class Sample
            {
            }

            partial class Sample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedNestedClass_WithoutDerivedType_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                {|MA0216:closed|} class Nested
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedGenericClass_WithDerivedType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            closed class Sample<T>;
            class Derived : Sample<int>;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedDerivedType_WithoutDerivedType_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            closed class Sample;
            {|MA0216:closed|} class Derived : Sample;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedClass_WithAbstractMember_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            {|MA0216:closed|} class Sample
            {
                public abstract int Value { get; }
            }
            """;

        return test.RunAsync();
    }
}
#endif

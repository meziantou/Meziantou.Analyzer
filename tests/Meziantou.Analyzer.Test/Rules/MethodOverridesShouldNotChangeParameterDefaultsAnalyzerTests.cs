using Microsoft.CodeAnalysis;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.MethodOverridesShouldNotChangeParameterDefaultsAnalyzer,
    Meziantou.Analyzer.Rules.MethodOverridesShouldNotChangeParameterDefaultsFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MethodOverridesShouldNotChangeParameterDefaultsAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    private static DiagnosticResult ExpectedChangedDefault(int markupKey, string original, string current) =>
        new DiagnosticResult(RuleIdentifiers.MethodOverridesShouldNotChangeParameterDefaults, DiagnosticSeverity.Warning)
            .WithLocation(markupKey)
            .WithMessage($"Method overrides should not change default values (original: {original}; current: {current})");

    [Fact]
    public Task Interface_ExplicitImplementation()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
                void A(int a = 0);
            }

            class Test : ITest
            {
                void ITest.A(int a) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_SameValue()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
                void A(int a = 0);
            }

            class Test : ITest
            {
                public void A(int a = 0) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Override_SameValue()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public virtual void A(int a = 0, int b = 1) { }
            }

            class TestDerived : Test
            {
                public override void A(int a = 0, int b = 1) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Override_DifferentValue()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public virtual void A(int a = 0, int b = 1) { }
            }

            class TestDerived : Test
            {
                public override void A(int {|#0:a|} = 1, int {|#1:b|} = 2) { }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedChangedDefault(0, "'0'", "'1'"));
        test.ExpectedDiagnostics.Add(ExpectedChangedDefault(1, "'1'", "'2'"));
        test.FixedCode = """
            class Test
            {
                public virtual void A(int a = 0, int b = 1) { }
            }

            class TestDerived : Test
            {
                public override void A(int a = 0, int b = 1) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task New_DifferentValue()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public virtual void A(int a = 0, int b = 1) { }
            }

            class TestDerived : Test
            {
                public void A(int a = 1, int b = 2) { } // no override
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Override_DifferentValue_OriginalParameterHasNoDefault()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public virtual void A(int a) { }
            }

            class TestDerived : Test
            {
                public override void A(int {|#0:a|} = 1) { }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedChangedDefault(0, "<no default value>", "'1'"));
        test.FixedCode = """
            class Test
            {
                public virtual void A(int a) { }
            }

            class TestDerived : Test
            {
                public override void A(int a) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Override_DifferentValue_OverrideParameterHasNoDefault()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public virtual void A(int a = 0) { }
            }

            class TestDerived : Test
            {
                public override void A(int {|#0:a|}) { }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedChangedDefault(0, "'0'", "<no default value>"));
        test.FixedCode = """
            class Test
            {
                public virtual void A(int a = 0) { }
            }

            class TestDerived : Test
            {
                public override void A(int a = 0) { }
            }
            """;

        return test.RunAsync();
    }
}

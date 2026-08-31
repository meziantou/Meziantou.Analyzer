using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseDefaultEqualsOnValueTypeAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseDefaultEqualsOnValueTypeAnalyzer_EqualsTests
{
    // This class covers MA0065 only, the way the original test filtered the diagnostics to that rule
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.DisabledDiagnostics.Add(RuleIdentifiers.StructWithDefaultEqualsImplementationUsedAsAKey);
        return test;
    }

    [Fact]
    public Task Equals_DefaultImplementation()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
            }

            class Sample
            {
                public void A()
                {
                    _ = {|MA0065:new Test().Equals(new Test())|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ObjectEquals_DefaultImplementation()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                public void A()
                {
                    _ = {|MA0065:Equals(new Test())|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_Override()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                public override bool Equals(object o) => throw null;
                public override int GetHashCode() => throw null;
            }

            class Sample
            {
                public void A()
                {
                    _ = new Test().Equals(new Test());
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetHashCode_DefaultImplementation()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
            }

            class Sample
            {
                public void A()
                {
                    _ = {|MA0065:new Test().GetHashCode()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetHashCode_Override()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                public override bool Equals(object o) => throw null;
                public override int GetHashCode() => throw null;
            }

            class Sample
            {
                public void A()
                {
                    _ = new Test().GetHashCode();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetHashCode_Enum()
    {
        var test = CreateTest();
        test.TestCode = """
            enum Test
            {
                A,
                B,
            }

            class Sample
            {
                public void A()
                {
                    _ = Test.A.GetHashCode();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetHashCode_EnumVariable()
    {
        var test = CreateTest();
        test.TestCode = """
            enum Test
            {
                A,
                B,
            }

            class Sample
            {
                public void A()
                {
                    var a = Test.A;
                    _ = a.GetHashCode();
                }
            }
            """;

        return test.RunAsync();
    }
}

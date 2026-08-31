using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptionalParametersAttributeAnalyzer,
    Meziantou.Analyzer.Rules.OptionalParametersAttributeFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptionalParametersAttributeAnalyzerMA0087Tests
{
    // This class covers MA0087 only, the way the original test filtered the diagnostics to that rule
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add(RuleIdentifiers.DefaultValueShouldNotBeUsedWhenParameterDefaultValueIsMeant);
        return test;
    }

    [Fact]
    public Task MissingOptionalAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.InteropServices;
            class Test
            {
                void A([DefaultParameterValue(10)]int {|MA0087:a|})
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingOptionalAttribute_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.InteropServices;
            class Test
            {
                void A([DefaultParameterValue(10)]int {|MA0087:a|})
                {
                }
            }
            """;
        test.FixedCode = """
            using System.Runtime.InteropServices;
            class Test
            {
                void A([Optional, DefaultParameterValue(10)]int a)
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task BothAttributes()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.InteropServices;
            class Test
            {
                void A([Optional, DefaultParameterValue(10)]int a)
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptionalAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.InteropServices;
            class Test
            {
                void A([Optional]int a)
                {
                }
            }
            """;

        return test.RunAsync();
    }
}

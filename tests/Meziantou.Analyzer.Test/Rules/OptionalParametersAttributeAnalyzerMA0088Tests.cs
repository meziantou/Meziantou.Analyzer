using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptionalParametersAttributeAnalyzer,
    Meziantou.Analyzer.Rules.OptionalParametersAttributeFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptionalParametersAttributeAnalyzerMA0088Tests
{
    // This class covers MA0088 only, the way the original test filtered the diagnostics to that rule
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add(RuleIdentifiers.ParametersWithDefaultValueShouldBeMarkedWithOptionalParameter);
        return test;
    }

    [Fact]
    public Task DefaultParameterValue()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.ComponentModel;
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
    public Task DefaultValue()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.ComponentModel;
            using System.Runtime.InteropServices;

            class Test
            {
                void A([DefaultValue(10)]int {|MA0088:a|})
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
            using System.ComponentModel;
            using System.Runtime.InteropServices;

            class Test
            {
                void A([Optional, DefaultParameterValue(10), DefaultValue(10)]int a)
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DefaultValue_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.ComponentModel;
            using System.Runtime.InteropServices;

            class Test
            {
                void A([DefaultValue(10)]int {|MA0088:a|})
                {
                }
            }
            """;
        test.FixedCode = """
            using System.ComponentModel;
            using System.Runtime.InteropServices;

            class Test
            {
                void A([DefaultValue(10), DefaultParameterValue(10)]int a)
                {
                }
            }
            """;

        return test.RunAsync();
    }
}

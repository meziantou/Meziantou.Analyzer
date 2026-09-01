using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptionalParametersAttributeAnalyzer,
    Meziantou.Analyzer.Rules.OptionalParametersAttributeFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptionalParametersAttributeAnalyzerTests
{
    [Fact]
    public Task MissingOptionalAttribute()
    {
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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

    [Fact]
    public Task DefaultParameterValue()
    {
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
    public Task OptionalAndDefaultParameterValueAndDefaultValueAttributes()
    {
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
        // Applying this fix reveals MA0087, and the test asserts the result of a single application
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne;
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
                void A([DefaultValue(10), DefaultParameterValue(10)]int {|MA0087:a|})
                {
                }
            }
            """;

        return test.RunAsync();
    }
}

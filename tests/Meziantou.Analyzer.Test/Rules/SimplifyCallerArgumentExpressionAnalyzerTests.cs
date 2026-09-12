using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.SimplifyCallerArgumentExpressionAnalyzer,
    Meziantou.Analyzer.Rules.SimplifyCallerArgumentExpressionFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public class SimplifyCallerArgumentExpressionAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        return test;
    }

    [Fact]
    public Task NotCSharp10()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp9;
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null) { }

                void A(string value)
                {
                    NotNull(value, "value");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null) { }

                void A(string value)
                {
                    NotNull(value.Length, {|MA0108:"value.Length"|});
                }
            }
            """;
        test.FixedCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null) { }

                void A(string value)
                {
                    NotNull(value.Length);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_NamedParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null, string extra = null) { }

                void A(string value)
                {
                    NotNull(value, {|MA0108:parameterName: "value"|}, "extra");
                }
            }
            """;
        test.FixedCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null, string extra = null) { }

                void A(string value)
                {
                    NotNull(value, extra: "extra");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_FollowedByPositionalArgument()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null, string extra = null) { }

                void A(string value)
                {
                    NotNull(value, {|MA0108:"value"|}, "extra");
                }
            }
            """;
        test.FixedCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null, string extra = null) { }

                void A(string value)
                {
                    NotNull(value, extra: "extra");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_FollowedByNamedArgument()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null, string extra = null) { }

                void A(string value)
                {
                    NotNull(value, {|MA0108:"value"|}, extra: "extra");
                }
            }
            """;
        test.FixedCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null, string extra = null) { }

                void A(string value)
                {
                    NotNull(value, extra: "extra");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_FollowedByPositionalArgument_ParameterNameIsKeyword()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null, string @class = null) { }

                void A(string value)
                {
                    NotNull(value, {|MA0108:"value"|}, "extra");
                }
            }
            """;
        test.FixedCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null, string @class = null) { }

                void A(string value)
                {
                    NotNull(value, @class: "extra");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_FollowedByExpandedParamsArguments_NoCodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null, params int[] values) { }

                void A(string value)
                {
                    NotNull(value, {|MA0108:"value"|}, 1, 2);
                }
            }
            """;
        test.FixedCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null, params int[] values) { }

                void A(string value)
                {
                    NotNull(value, {|MA0108:"value"|}, 1, 2);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotSameValue()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null) { }

                void A(string value)
                {
                    NotNull(value, "value2");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValueNotConstant()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void NotNull(object? target, [CallerArgumentExpression("target")] string? parameterName = null) { }

                void A(string value)
                {
                    NotNull(value, value);
                }
            }
            """;

        return test.RunAsync();
    }
}

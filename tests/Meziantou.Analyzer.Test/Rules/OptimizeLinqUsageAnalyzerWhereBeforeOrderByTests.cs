using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptimizeLinqUsageAnalyzer,
    Meziantou.Analyzer.Rules.OptimizeLinqUsageFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerWhereBeforeOrderByTests
{
    // This class covers MA0063 only, the way the original test filtered the diagnostics to that rule
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_UseOrder);
        return test;
    }

    private static DiagnosticResult ExpectedWhereBefore(string method) =>
        new DiagnosticResult(RuleIdentifiers.OptimizeEnumerable_WhereBeforeOrderBy, DiagnosticSeverity.Info)
            .WithLocation(0)
            .WithMessage($"Call 'Where' before '{method}'");

    [Theory]
    [InlineData("OrderBy")]
    [InlineData("OrderByDescending")]
    public Task Enumerable_WhereBeforeOrderBy_Valid(string a)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    System.Collections.Generic.IEnumerable<string> enumerable = null;
                    enumerable.Where(x => x != null).{{a}}(x => x != null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Order")]
    [InlineData("OrderDescending")]
    public Task Enumerable_WhereBeforeOrder_Valid(string a)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    System.Collections.Generic.IEnumerable<string> enumerable = null;
                    enumerable.Where(x => x != null).{{a}}();
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("OrderBy")]
    [InlineData("OrderByDescending")]
    public Task Enumerable_WhereAfterOrderBy_Invalid(string a)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    System.Collections.Generic.IEnumerable<string> enumerable = null;
                    {|#0:enumerable.{{a}}(x => x).Where(x => x != null)|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedWhereBefore(a));
        test.FixedCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    System.Collections.Generic.IEnumerable<string> enumerable = null;
                    enumerable.Where(x => x != null).{{a}}(x => x);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Order")]
    [InlineData("OrderDescending")]
    public Task Enumerable_WhereAfterOrder_Invalid(string a)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    System.Collections.Generic.IEnumerable<string> enumerable = null;
                    {|#0:enumerable.{{a}}().Where(x => x != null)|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedWhereBefore(a));
        test.FixedCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    System.Collections.Generic.IEnumerable<string> enumerable = null;
                    enumerable.Where(x => x != null).{{a}}();
                }
            }
            """;

        return test.RunAsync();
    }
}

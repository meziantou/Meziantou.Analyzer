using Microsoft.CodeAnalysis;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptimizeLinqUsageAnalyzer,
    Meziantou.Analyzer.Rules.OptimizeLinqUsageFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerDuplicateOrderByTests
{
    // This class covers MA0030 only, the way the original test filtered the diagnostics to that rule
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_UseOrder);
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_WhereBeforeOrderBy);
        return test;
    }

    private static DiagnosticResult ExpectedDuplicateOrderBy(string method, string expectedMethod) =>
        new DiagnosticResult(RuleIdentifiers.DuplicateEnumerable_OrderBy, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithMessage($"Remove the first '{method}' method or use '{expectedMethod}'");

    [Theory]
    [InlineData("OrderBy", "OrderBy", "ThenBy")]
    [InlineData("OrderBy", "OrderByDescending", "ThenByDescending")]
    [InlineData("OrderByDescending", "OrderBy", "ThenBy")]
    [InlineData("OrderByDescending", "OrderByDescending", "ThenByDescending")]
    public Task IQueryable_TwoOrderBy_FixRemoveDuplicate(string a, string b, string expectedMethod)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IQueryable<string> query = null;
                    {|#0:query.{{a}}(x => x).{{b}}(x => x)|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedDuplicateOrderBy(a, expectedMethod));
        test.CodeActionIndex = 1;
        test.FixedCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IQueryable<string> query = null;
                    query.{{b}}(x => x);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("OrderBy", "OrderBy", "ThenBy")]
    [InlineData("OrderBy", "OrderByDescending", "ThenByDescending")]
    [InlineData("OrderByDescending", "OrderBy", "ThenBy")]
    [InlineData("OrderByDescending", "OrderByDescending", "ThenByDescending")]
    public Task TwoOrderBy_FixRemoveDuplicate(string a, string b, string expectedMethod)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = Enumerable.Empty<int>();
                    {|#0:enumerable.{{a}}(x => x).{{b}}(x => x)|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedDuplicateOrderBy(a, expectedMethod));
        test.CodeActionIndex = 1;
        test.FixedCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = Enumerable.Empty<int>();
                    enumerable.{{b}}(x => x);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("OrderBy", "OrderBy", "ThenBy")]
    [InlineData("OrderBy", "OrderByDescending", "ThenByDescending")]
    [InlineData("OrderByDescending", "OrderBy", "ThenBy")]
    [InlineData("OrderByDescending", "OrderByDescending", "ThenByDescending")]
    public Task TwoOrderBy_FixWithThenBy(string a, string b, string expectedMethod)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = Enumerable.Empty<int>();
                    {|#0:enumerable.{{a}}(x => x).{{b}}(x => x)|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedDuplicateOrderBy(a, expectedMethod));
        test.FixedCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = Enumerable.Empty<int>();
                    enumerable.{{a}}(x => x).{{expectedMethod}}(x => x);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("ThenBy", "OrderBy", "ThenBy")]
    [InlineData("ThenByDescending", "OrderBy", "ThenBy")]
    [InlineData("ThenBy", "OrderByDescending", "ThenByDescending")]
    [InlineData("ThenByDescending", "OrderByDescending", "ThenByDescending")]
    public Task ThenByFollowedByOrderBy(string a, string b, string expectedMethod)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = Enumerable.Empty<int>();
                    {|#0:enumerable.OrderBy(x => x).{{a}}(x => x).{{b}}(x => x)|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedDuplicateOrderBy(a, expectedMethod));
        test.FixedCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = Enumerable.Empty<int>();
                    enumerable.OrderBy(x => x).{{a}}(x => x).{{expectedMethod}}(x => x);
                }
            }
            """;

        return test.RunAsync();
    }
}

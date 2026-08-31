using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptimizeLinqUsageAnalyzer,
    Meziantou.Analyzer.Rules.OptimizeLinqUsageFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerCombineLinqMethodsTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add("MA0020");
        test.DisabledDiagnostics.Add("MA0030");
        test.DisabledDiagnostics.Add("MA0031");
        test.DisabledDiagnostics.Add("MA0063");
        test.DisabledDiagnostics.Add("MA0078");
        test.DisabledDiagnostics.Add("MA0098");
        test.DisabledDiagnostics.Add("MA0112");
        test.DisabledDiagnostics.Add("MA0159");
        return test;
    }


    [Theory]
    [InlineData("Any")]
    [InlineData("First")]
    [InlineData("FirstOrDefault")]
    [InlineData("Last")]
    [InlineData("LastOrDefault")]
    [InlineData("Single")]
    [InlineData("SingleOrDefault")]
    [InlineData("Count")]
    [InlineData("LongCount")]
    public Task CombineWhereWithTheFollowingMethod(string methodName)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    {|#0:enumerable.Where(x => x == 0).{{methodName}}()|};
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0029", DiagnosticSeverity.Info).WithLocation(0).WithMessage($"Combine 'Where' with '{methodName}'"));
        test.FixedCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    enumerable.{{methodName}}(x => x == 0);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CombineWhereWithTheFollowingWhereMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    {|#0:enumerable.Where(x => x == 0).Where(y => true)|};
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0029", DiagnosticSeverity.Info).WithLocation(0).WithMessage($"Combine 'Where' with 'Where'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    enumerable.Where(x => x == 0 && true);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CombineWhereWithTheFollowingWhereMethod_ExpressionWithPredicate()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq;
            using System.Linq.Expressions;
            class Test
            {
                public Test(Expression<Func<int, bool>> predicate)
                {
                    IQueryable<int> queryable = null!;
                    queryable.Where(x => x == 0).Where(predicate);
                    queryable.Where(predicate).Where(x => x == 0);
                }
            }

            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Any")]
    [InlineData("First")]
    [InlineData("FirstOrDefault")]
    [InlineData("Last")]
    [InlineData("LastOrDefault")]
    [InlineData("Single")]
    [InlineData("SingleOrDefault")]
    [InlineData("Count")]
    [InlineData("LongCount")]
    public Task CombineWhereWithTheFollowingMethod_IQueryable(string methodName)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    System.Linq.IQueryable<int> enumerable = null;
                    enumerable.Where(x => x == 0).{{methodName}}();
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CombineWhereWithTheFollowingWhereMethod_IQueryable()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    System.Linq.IQueryable<int> enumerable = null;
                    enumerable.Where(x => x == 0).Where(y => true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CombineWhereWithTheFollowingMethod_CombineLambdaWithNothing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    {|#0:enumerable.Where(x => x == 0 || x == 1).Any()|};
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0029", DiagnosticSeverity.Info).WithLocation(0).WithMessage($"Combine 'Where' with 'Any'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    enumerable.Any(x => x == 0 || x == 1);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CombineWhereWithTheFollowingMethod_CombineLambdaWithLambda()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    {|#0:enumerable.Where(x => x == 0 || x == 1).Any(y => y == 2)|};
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0029", DiagnosticSeverity.Info).WithLocation(0).WithMessage($"Combine 'Where' with 'Any'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    enumerable.Any(x => (x == 0 || x == 1) && x == 2);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CombineWhereWithTheFollowingMethod_CombineMethodGroupWithNothing()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    {|#0:enumerable.Where(Filter).Any()|};
                }

                bool Filter(int x) => true;
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0029", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Combine 'Where' with 'Any'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    enumerable.Any(Filter);
                }

                bool Filter(int x) => true;
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CombineWhereWithTheFollowingMethod_CombineMethodGroupWithMethodGroup()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    {|#0:enumerable.Where(Filter).Any(Filter)|};
                }

                bool Filter(int x) => true;
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0029", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Combine 'Where' with 'Any'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    enumerable.Any(x => Filter(x) && Filter(x));
                }

                bool Filter(int x) => true;
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CombineWhereWithTheFollowingMethod_CombineMethodGroupWithLambda()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    {|#0:enumerable.Where(Filter).Any(x => true)|};
                }

                bool Filter(int x) => true;
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0029", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Combine 'Where' with 'Any'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    enumerable.Any(x => Filter(x) && true);
                }

                bool Filter(int x) => true;
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CombineWhereWithAny_DoNotReportForWhereWithIndex()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    enumerable.Where(Filter).Any(x => true);
                }

                bool Filter(int x, int index) => true;
            }

            """;

        return test.RunAsync();
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptimizeLinqUsageAnalyzer,
    Meziantou.Analyzer.Rules.OptimizeLinqUsageFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerCountTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add("MA0020");
        test.DisabledDiagnostics.Add("MA0029");
        test.DisabledDiagnostics.Add("MA0030");
        test.DisabledDiagnostics.Add("MA0063");
        test.DisabledDiagnostics.Add("MA0078");
        test.DisabledDiagnostics.Add("MA0098");
        test.DisabledDiagnostics.Add("MA0112");
        test.DisabledDiagnostics.Add("MA0159");
        return test;
    }

    [Theory]
    [InlineData("enumerable.Count() < 0")]
    [InlineData("enumerable.Count() <= -1")]
    [InlineData("enumerable.Count() <= -2")]
    [InlineData("enumerable.Count() == -1")]
    [InlineData("-1 == enumerable.Count()")]
    public Task Count_AlwaysFalse(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = Enumerable.Empty<int>();
                    _ = {|#0:{{text}}|};
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0031", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Expression is always false"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = Enumerable.Empty<int>();
                    _ = false;
                }
            }

            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("enumerable.Count() != -2")]
    [InlineData("enumerable.Count() > -1")]
    [InlineData("enumerable.Count() >= 0")]
    [InlineData("-10 <= enumerable.Count()")]
    public Task Count_AlwaysTrue(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    int n = 10;
                    var enumerable = Enumerable.Empty<int>();
                    _ = {|#0:{{text}}|};
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0031", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Expression is always true"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    int n = 10;
                    var enumerable = Enumerable.Empty<int>();
                    _ = true;
                }
            }

            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Count() == 0", "Replace 'Count() == 0' with 'Any() == false'")]
    [InlineData("Count() < 1", "Replace 'Count() < 1' with 'Any() == false'")]
    [InlineData("Count() <= 0", "Replace 'Count() <= 0' with 'Any() == false'")]
    public Task Count_AnyFalse(string text, string expectedMessage)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    _ = {|#0:enumerable.{{text}}|};
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0031", DiagnosticSeverity.Info).WithLocation(0).WithMessage(expectedMessage));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    _ = !enumerable.Any();
                }
            }

            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Count() != 0", "Replace 'Count() != 0' with 'Any()'")]
    [InlineData("Count() > 0", "Replace 'Count() > 0' with 'Any()'")]
    [InlineData("Count() >= 1", "Replace 'Count() >= 1' with 'Any()'")]
    public Task Count_AnyTrue(string text, string expectedMessage)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    _ = {|#0:enumerable.{{text}}|};
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0031", DiagnosticSeverity.Info).WithLocation(0).WithMessage(expectedMessage));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    _ = enumerable.Any();
                }
            }

            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Count() == 1", "Take(2).Count() == 1", "Replace 'Count() == 1' with 'Take(2).Count() == 1'")]
    [InlineData("Count() != 10", "Take(11).Count() != 10", "Replace 'Count() != 10' with 'Take(11).Count() != 10'")]
    [InlineData("Count() != n", "Take(n + 1).Count() != n", "Replace 'Count() != n' with 'Take(n + 1).Count() != n'")]
    [InlineData("Count(x => x > 1) != n", "Where(x => x > 1).Take(n + 1).Count() != n", "Replace 'Count() != n' with 'Take(n + 1).Count() != n'")]
    public Task Count_TakeAndCount(string text, string fix, string expectedMessage)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    int n = 10;
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    _ = {|#0:enumerable.{{text}}|};
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0031", DiagnosticSeverity.Info).WithLocation(0).WithMessage(expectedMessage));
        test.FixedCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    int n = 10;
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    _ = enumerable.{{fix}};
                }
            }

            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Count() > 1", "Skip(1).Any()", "Replace 'Count() > 1' with 'Skip(1).Any()'")]
    [InlineData("Count() > 2", "Skip(2).Any()", "Replace 'Count() > 2' with 'Skip(2).Any()'")]
    [InlineData("Count() > n", "Skip(n).Any()", "Replace 'Count() > n' with 'Skip(n).Any()'")]
    [InlineData("Count() >= 2", "Skip(1).Any()", "Replace 'Count() >= 2' with 'Skip(1).Any()'")]
    [InlineData("Count() >= n", "Skip(n - 1).Any()", "Replace 'Count() >= n' with 'Skip(n - 1).Any()'")]
    public Task Count_SkipAndAny(string text, string fix, string expectedMessage)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    int n = 10;
                    var enumerable = Enumerable.Empty<int>();
                    _ = {|#0:enumerable.{{text}}|};
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0031", DiagnosticSeverity.Info).WithLocation(0).WithMessage(expectedMessage));
        test.FixedCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    int n = 10;
                    var enumerable = Enumerable.Empty<int>();
                    _ = enumerable.{{fix}};
                }
            }

            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Count() < 2", "Skip(1).Any()", "Replace 'Count() < 2' with 'Skip(1).Any() == false'")]
    [InlineData("Count() < n", "Skip(n - 1).Any()", "Replace 'Count() < n' with 'Skip(n - 1).Any() == false'")]
    [InlineData("Count() <= 1", "Skip(1).Any()", "Replace 'Count() <= 1' with 'Skip(1).Any() == false'")]
    [InlineData("Count() <= 2", "Skip(2).Any()", "Replace 'Count() <= 2' with 'Skip(2).Any() == false'")]
    [InlineData("Count() <= n", "Skip(n).Any()", "Replace 'Count() <= n' with 'Skip(n).Any() == false'")]
    [InlineData("Count(x => true) <= n", "Where(x => true).Skip(n).Any()", "Replace 'Count() <= n' with 'Skip(n).Any() == false'")]
    public Task Count_NotSkipAndAny(string text, string fix, string expectedMessage)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    int n = 10;
                    var enumerable = Enumerable.Empty<int>();
                    _ = {|#0:enumerable.{{text}}|};
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0031", DiagnosticSeverity.Info).WithLocation(0).WithMessage(expectedMessage));
        test.FixedCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    int n = 10;
                    var enumerable = Enumerable.Empty<int>();
                    _ = !enumerable.{{fix}};
                }
            }

            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Take(10).Count() == 1")]
    public Task Count_Equals(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    _ = enumerable.{{text}};
                }
            }

            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Take(1).Count() != n")]
    public Task Count_NotEquals(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    int n = 10;
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    _ = enumerable.{{text}};
                }
            }

            """;

        return test.RunAsync();
    }
}

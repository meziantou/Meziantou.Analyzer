using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptimizeLinqUsageAnalyzer,
    Meziantou.Analyzer.Rules.OptimizeLinqUsageFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerTests
{
    private static DiagnosticResult ExpectedUseOrder(string message) =>
        new DiagnosticResult(RuleIdentifiers.OptimizeEnumerable_UseOrder, DiagnosticSeverity.Info)
            .WithLocation(0)
            .WithMessage(message);

    private static DiagnosticResult ExpectedDuplicateOrderBy(string method, string expectedMethod) =>
        new DiagnosticResult(RuleIdentifiers.DuplicateEnumerable_OrderBy, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithMessage($"Remove the first '{method}' method or use '{expectedMethod}'");

    private static DiagnosticResult ExpectedWhereBefore(string method) =>
        new DiagnosticResult(RuleIdentifiers.OptimizeEnumerable_WhereBeforeOrderBy, DiagnosticSeverity.Info)
            .WithLocation(0)
            .WithMessage($"Call 'Where' before '{method}'");

    [Fact]
    public Task FirstOrDefaultAsync_Net9()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    var list = new System.Collections.Generic.List<int>();
                    list.FirstOrDefault();
                    list.FirstOrDefault(x => x == 0);
                    enumerable.FirstOrDefault();
                    enumerable.FirstOrDefault(x => x == 0);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FirstOrDefaultAsync()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    var list = new System.Collections.Generic.List<int>();
                    list.FirstOrDefault();
                    list.{|#0:FirstOrDefault|}(x => x == 0);
                    enumerable.FirstOrDefault();
                    enumerable.FirstOrDefault(x => x == 0);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use 'Find()' instead of 'FirstOrDefault()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    var list = new System.Collections.Generic.List<int>();
                    list.FirstOrDefault();
                    list.Find(x => x == 0);
                    enumerable.FirstOrDefault();
                    enumerable.FirstOrDefault(x => x == 0);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FirstOrDefaultAsync_Cast()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    System.Func<int, bool> predicate = _ => true;
                    list.FirstOrDefault(predicate);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FirstOrDefaultAsync_Cast_ConfigureEnabled()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestState.SetConfiguration("MA0020.report_when_conversion_needed", "true");
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    System.Func<int, bool> predicate = _ => true;
                    list.{|#0:FirstOrDefault|}(predicate);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use 'Find()' instead of 'FirstOrDefault()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    System.Func<int, bool> predicate = _ => true;
                    list.Find(new System.Predicate<int>(predicate));
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TrueForAll()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    var list = new System.Collections.Generic.List<int>();
                    list.{|#0:All|}(x => x == 0);
                    enumerable.All(x => x == 0);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use 'TrueForAll()' instead of 'All()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    var list = new System.Collections.Generic.List<int>();
                    list.TrueForAll(x => x == 0);
                    enumerable.All(x => x == 0);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TrueForAll_Cast()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    System.Func<int, bool> predicate = _ => true;
                    list.All(predicate);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TrueForAll_Cast_ConfigureEnabled()
    {
        var test = new CodeFixTest();
        test.TestState.SetConfiguration("MA0020.report_when_conversion_needed", "true");
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    System.Func<int, bool> predicate = _ => true;
                    list.{|#0:All|}(predicate);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use 'TrueForAll()' instead of 'All()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    System.Func<int, bool> predicate = _ => true;
                    list.TrueForAll(new System.Predicate<int>(predicate));
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Exists()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    var list = new System.Collections.Generic.List<int>();
                    list.{|#0:Any|}(x => x == 0);
                    enumerable.Any(x => x == 0);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use 'Exists()' instead of 'Any()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    var list = new System.Collections.Generic.List<int>();
                    list.Exists(x => x == 0);
                    enumerable.Any(x => x == 0);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Exists_Cast()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    System.Func<int, bool> predicate = _ => true;
                    list.Any(predicate);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Exists_Cast_ConfigureEnabled()
    {
        var test = new CodeFixTest();
        test.TestState.SetConfiguration("MA0020.report_when_conversion_needed", "true");
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    System.Func<int, bool> predicate = _ => true;
                    list.{|#0:Any|}(predicate);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use 'Exists()' instead of 'Any()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    System.Func<int, bool> predicate = _ => true;
                    list.Exists(new System.Predicate<int>(predicate));
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Count_IEnumerableAsync()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = System.Linq.Enumerable.Empty<int>();
                    enumerable.Count();
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Count_ListAsync()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    _ = list.{|#0:Count|}();
                    list.Count(x => x == 0);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use 'Count' instead of 'Count()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    _ = list.Count;
                    list.Count(x => x == 0);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Count_ICollectionExplicitImplementationAsync()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections;
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new Collection<int>();
                    list.Count();
                    list.Count(x => x == 0);
                }

                private class Collection<T> : ICollection<T>
                {
                    int ICollection<T>.Count => throw null;
                    bool ICollection<T>.IsReadOnly => throw null;
                    void ICollection<T>.Add(T item) => throw null;
                    void ICollection<T>.Clear() => throw null;
                    bool ICollection<T>.Contains(T item) => throw null;
                    void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw null;
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                    bool ICollection<T>.Remove(T item) => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Count_ArrayAsync()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new int[10];
                    _ = list.{|#0:Count|}();
                    list.Count(x => x == 0);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use 'Length' instead of 'Count()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new int[10];
                    _ = list.Length;
                    list.Count(x => x == 0);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Count_VariableTypedAsEnumerableAssignedToList()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<int> list = new List<int>();
                    _ = list.Count();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Exists_VariableTypedAsEnumerableAssignedToList()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<int> list = new List<int>();
                    _ = list.Any(x => x == 0);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("enumerable.Count() < 0")]
    [InlineData("enumerable.Count() <= -1")]
    [InlineData("enumerable.Count() <= -2")]
    [InlineData("enumerable.Count() == -1")]
    [InlineData("-1 == enumerable.Count()")]
    public Task Count_AlwaysFalse(string text)
    {
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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

    [Fact]
    public Task Any_List()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var collection = new System.Collections.Generic.List<int>();
                    _ = {|MA0112:collection.Any()|};
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Any_List_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var collection = new System.Collections.Generic.List<int>();
                    _ = {|MA0112:collection.Any()|};
                }
            }

            """;
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var collection = new System.Collections.Generic.List<int>();
                    _ = collection.Count != 0;
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Any_Array()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var collection = new int[10];
                    _ = {|MA0112:collection.Any()|};
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Any_HashSet()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var collection = new System.Collections.Generic.HashSet<int>();
                    _ = {|MA0112:collection.Any()|};
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Any_Dictionary()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var collection = new System.Collections.Generic.Dictionary<int, int>();
                    _ = {|MA0112:collection.Any()|};
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Any_Enumerable()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var collection = Enumerable.Empty<int>();
                    _ = collection.Any();
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Any_Expression_Array()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var collection = new int[10];
                    _ = collection.Any(i => i > 1);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Any_VariableTypedAsEnumerableAssignedToList()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                void A()
                {
                    IEnumerable<int> collection = new List<int>();
                    _ = collection.Any();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Any_ExplicitCastToEnumerable()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                void A(List<int> collection)
                {
                    _ = ((IEnumerable<int>)collection).Any();
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
    public Task CombineWhereWithTheFollowingMethod(string methodName)
    {
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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

    [Theory]
    [InlineData("source.{|MA0078:Select|}(dt => (BaseType)dt)",
                "source.Cast<BaseType>()")]
    [InlineData("Enumerable.{|MA0078:Select|}(source, dt => (Test.BaseType)dt).FirstOrDefault()",
                "source.Cast<BaseType>().FirstOrDefault()")]
    [InlineData("System.Linq.Enumerable.Empty<DerivedType>().{|MA0078:Select|}(dt => (Gen.IList<string>)dt)",
                            "Enumerable.Empty<DerivedType>().Cast<Gen.IList<string>>()")]
    [InlineData("Enumerable.Range(0, 1).{|MA0078:Select<int, object>|}(i => i)",
                "Enumerable.Range(0, 1).Cast<object>()")]
    [InlineData("source.{|MA0078:Select|}(i => (object?)i)",
                "source.Cast<object?>()",
                true)]
    [InlineData("source.{|MA0078:Select|}(i => (object)i)",
                "source.Cast<object>()",
                true)]
    [InlineData("source.{|MA0078:Select<DerivedType, object?>|}(i => i)",
                "source.Cast<object?>()",
                true)]
    [InlineData("source.{|MA0078:Select<DerivedType, object>|}(i => i)",
                "source.Cast<object>()",
                true)]
    public Task OptimizeLinq_WhenSelectorReturnsCastElement_ReplacesSelectByCast(
        string selectInvocation,
        string expectedReplacement,
        bool enableNullable = false)
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            #nullable {{(enableNullable ? "enable" : "disable")}}
            using System.Linq;
            using Gen = System.Collections.Generic;

            class Test
            {
                class BaseType { public string Name { get; set; } }
                class DerivedType : BaseType {}

                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<DerivedType>();
                    {{selectInvocation}};
                }
            }
            """;
        test.FixedCode = $$"""
            #nullable {{(enableNullable ? "enable" : "disable")}}
            using System.Linq;
            using Gen = System.Collections.Generic;

            class Test
            {
                class BaseType { public string Name { get; set; } }
                class DerivedType : BaseType {}

                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<DerivedType>();
                    {{expectedReplacement}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("source.Select(dt => dt.Name)")]            // No cast
    [InlineData("source.Select(dt => (object)dt.Name)")]    // Cast of property, not of element itself
    [InlineData("source.Select(dt => dt as BaseType)")]     // 'as' operator should not be replaced by Cast<>
    public Task OptimizeLinq_WhenSelectorDoesNotReturnCastElement_NoDiagnosticReported(string selectInvocation)
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                class BaseType { public string Name { get; set; } }
                class DerivedType : BaseType {}

                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<DerivedType>();
                    {{selectInvocation}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptimizeLinq_ExplicitCastIsRequired()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<int>();
                    source.Select(item => (byte)item);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("IssueId", "https://github.com/meziantou/Meziantou.Analyzer/issues/176")]
    public Task OptimizeLinq_UserDefinedImplicitOperator()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;
            using System.Linq;

            static class P
            {
                static void Main()
                {
                    var foos = new[] { new Foo("1"), new Foo("42") };
                    foreach (var i in foos.Select(x => (int)x))
                    {
                        Console.WriteLine(i);
                    }
                }
            }

            class Foo
            {
                private readonly string _value;
                public Foo(string value) => _value = value;

                public static implicit operator int(Foo foo) => int.Parse(foo._value, System.Globalization.CultureInfo.InvariantCulture);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("IssueId", "https://github.com/meziantou/Meziantou.Analyzer/issues/176")]
    public Task OptimizeLinq_UserDefinedImplicitOperator_ImplicitUse()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;
            using System.Linq;

            static class P
            {
                static void Main()
                {
                    var foos = new[] { new Foo("1"), new Foo("42") };
                    foreach (var i in foos.Select<Foo, int>(x => x))
                    {
                        Console.WriteLine(i);
                    }
                }
            }

            class Foo
            {
                private readonly string _value;
                public Foo(string value) => _value = value;

                public static implicit operator int(Foo foo) => int.Parse(foo._value, System.Globalization.CultureInfo.InvariantCulture);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptimizeLinq_UserDefinedExplicitOperator()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;
            using System.Linq;

            static class P
            {
                static void Main()
                {
                    var foos = new[] { new Foo("1"), new Foo("42") };
                    foreach (var i in foos.Select(x => (int)x))
                    {
                        Console.WriteLine(i);
                    }
                }
            }

            class Foo
            {
                private readonly string _value;
                public Foo(string value) => _value = value;

                public static explicit operator int(Foo foo) => int.Parse(foo._value, System.Globalization.CultureInfo.InvariantCulture);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptimizeLinq_IntToObject()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<int>();
                    source.{|MA0078:Select|}(item => (System.Object)item);
                }
            }
            """;
        test.FixedCode = """
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<int>();
                    source.Cast<object>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptimizeLinq_IntEnumToByte()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            using System.Collections.Generic;

            enum TestEnum
            {
                A,
                B,
            }

            class Test
            {
                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<TestEnum>();
                    source.Select(item => (byte)item);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptimizeLinq_ByteEnumToByte()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            using System.Collections.Generic;

            enum TestEnum : System.Byte
            {
                A,
                B,
            }

            class Test
            {
                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<TestEnum>();
                    source.{|MA0078:Select|}(item => (System.Byte)item);
                }
            }
            """;
        test.FixedCode = """
            using System.Linq;
            using System.Collections.Generic;

            enum TestEnum : System.Byte
            {
                A,
                B,
            }

            class Test
            {
                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<TestEnum>();
                    source.Cast<byte>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ElementAt_ListAsync()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    _ = list.{|#0:ElementAt|}(10);
                    list.ElementAtOrDefault(10);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseIndexerInsteadOfElementAt, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use '[]' instead of 'ElementAt()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    _ = list[10];
                    list.ElementAtOrDefault(10);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ElementAt_ArrayAsync()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new int[5];
                    _ = list.{|#0:ElementAt|}(10);
                    list.ElementAtOrDefault(10);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseIndexerInsteadOfElementAt, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use '[]' instead of 'ElementAt()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new int[5];
                    _ = list[10];
                    list.ElementAtOrDefault(10);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task First_ArrayAsync()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new int[5];
                    _ = list.{|#0:First|}();
                    list.First(x=> x == 0);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseIndexerInsteadOfElementAt, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use '[]' instead of 'First()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new int[5];
                    _ = list[0];
                    list.First(x=> x == 0);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Last_Array()
    {
        var test = new CodeFixTest();
        test.LanguageVersion = LanguageVersion.CSharp7_3;
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new int[5];
                    _ = list.{|#0:Last|}();
                    list.First(x=> x == 0);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseIndexerInsteadOfElementAt, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use '[]' instead of 'Last()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new int[5];
                    _ = list[list.Length - 1];
                    list.First(x=> x == 0);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Last_Array_CSharp8_IndexNotAvailable()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Default;
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new int[5];
                    _ = list.{|#0:Last|}();
                    list.First(x=> x == 0);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseIndexerInsteadOfElementAt, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use '[]' instead of 'Last()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new int[5];
                    _ = list[list.Length - 1];
                    list.First(x=> x == 0);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Last_Array_CSharp8_IndexAvailable()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new int[5];
                    _ = list.{|#0:Last|}();
                    list.First(x=> x == 0);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseIndexerInsteadOfElementAt, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use '[]' instead of 'Last()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new int[5];
                    _ = list[^1];
                    list.First(x=> x == 0);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Last_List()
    {
        var test = new CodeFixTest();
        test.LanguageVersion = LanguageVersion.CSharp7_3;
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    _ = list.{|#0:Last|}();
                    list.First(x=> x == 0);
                }
            }

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.UseIndexerInsteadOfElementAt, DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use '[]' instead of 'Last()'"));
        test.FixedCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var list = new System.Collections.Generic.List<int>();
                    _ = list[list.Count - 1];
                    list.First(x=> x == 0);
                }
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ElementAt_VariableTypedAsEnumerableAssignedToList()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<int> list = new List<int>();
                    _ = list.ElementAt(0);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IEnumerable_Order_net5()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net50;
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<string> query = null;
                    query.OrderBy(x => x);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IEnumerable_Order_LambdaNotValid()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<string> query = null;
                    query.OrderBy(x => true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IEnumerable_Order_LambdaReferenceAnotherParameter()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test(int a)
                {
                    IEnumerable<string> query = null;
                    query.OrderBy(x => a);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IEnumerable_Order()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<string> query = null;
                    query.{|#0:OrderBy|}(x => x);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedUseOrder("Use 'Order' instead of 'OrderBy'"));
        test.FixedCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<string> query = null;
                    query.Order();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IEnumerable_OrderDescending()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<string> query = null;
                    query.{|#0:OrderByDescending|}(x => x);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedUseOrder("Use 'OrderDescending' instead of 'OrderByDescending'"));
        test.FixedCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<string> query = null;
                    query.OrderDescending();
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
    public Task IQueryable_TwoOrderBy_FixRemoveDuplicate(string a, string b, string expectedMethod)
    {
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = Enumerable.Empty<int>();
                    {|#0:enumerable.{{a}}(x => -x).{{b}}(x => -x)|};
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
                    enumerable.{{b}}(x => -x);
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
        var test = new CodeFixTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = Enumerable.Empty<int>();
                    {|#0:enumerable.{{a}}(x => -x).{{b}}(x => -x)|};
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
                    enumerable.{{a}}(x => -x).{{expectedMethod}}(x => -x);
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
        var test = new CodeFixTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    var enumerable = Enumerable.Empty<int>();
                    {|#0:enumerable.OrderBy(x => -x).{{a}}(x => -x).{{b}}(x => -x)|};
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
                    enumerable.OrderBy(x => -x).{{a}}(x => -x).{{expectedMethod}}(x => -x);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("OrderBy")]
    [InlineData("OrderByDescending")]
    public Task Enumerable_WhereBeforeOrderBy_Valid(string a)
    {
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                public Test()
                {
                    System.Collections.Generic.IEnumerable<string> enumerable = null;
                    {|#0:enumerable.{{a}}(x => x.Length).Where(x => x != null)|};
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
                    enumerable.Where(x => x != null).{{a}}(x => x.Length);
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
        var test = new CodeFixTest();
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

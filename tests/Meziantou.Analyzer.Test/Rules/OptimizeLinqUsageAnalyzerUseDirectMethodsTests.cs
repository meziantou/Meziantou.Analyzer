using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptimizeLinqUsageAnalyzer,
    Meziantou.Analyzer.Rules.OptimizeLinqUsageFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerUseDirectMethodsTests
{
    // This class covers MA0020 only, the way the original test filtered the diagnostics to that rule
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_UseOrder);
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_WhereBeforeOrderBy);
        test.DisabledDiagnostics.Add(RuleIdentifiers.DuplicateEnumerable_OrderBy);
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_Count);
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_CombineMethods);
        test.DisabledDiagnostics.Add(RuleIdentifiers.UseIndexerInsteadOfElementAt);
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_CastInsteadOfSelect);
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_UseCountInsteadOfAny);
        return test;
    }

    [Fact]
    public Task FirstOrDefaultAsync_Net9()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
}

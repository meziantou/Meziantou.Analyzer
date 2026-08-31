using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptimizeLinqUsageAnalyzer,
    Meziantou.Analyzer.Rules.OptimizeLinqUsageFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerUseIndexerTests
{
    // This class covers MA0098 only, the way the original test filtered the diagnostics to that rule
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_UseOrder);
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_WhereBeforeOrderBy);
        test.DisabledDiagnostics.Add(RuleIdentifiers.DuplicateEnumerable_OrderBy);
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_Count);
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_CombineMethods);
        test.DisabledDiagnostics.Add(RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods);
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_CastInsteadOfSelect);
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_UseCountInsteadOfAny);
        return test;
    }

    [Fact]
    public Task ElementAt_ListAsync()
    {
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp8;
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
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp8;
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
        var test = CreateTest();
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
        var test = CreateTest();
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
}

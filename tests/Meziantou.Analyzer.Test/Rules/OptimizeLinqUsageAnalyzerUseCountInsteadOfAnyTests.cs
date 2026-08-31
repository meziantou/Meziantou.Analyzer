using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptimizeLinqUsageAnalyzer,
    Meziantou.Analyzer.Rules.OptimizeLinqUsageFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerUseCountInsteadOfAnyTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task Any_List()
    {
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
}

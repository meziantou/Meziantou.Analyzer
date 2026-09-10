using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.AvoidEnumerableContainsOnSetAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AvoidEnumerableContainsOnSetAnalyzerTests
{
    [Fact]
    public Task HashSet_Contains_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Collections.Generic;
                using System.Linq;

                class Sample
                {
                    bool Test(HashSet<string> set, string value) => set.Contains(value);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task HashSet_EnumerableContains_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Collections.Generic;
                using System.Linq;

                class Sample
                {
                    bool Test(HashSet<string> set, string value) => Enumerable.Contains(set, value);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task IEnumerable_Contains_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Collections.Generic;
                using System.Linq;

                class Sample
                {
                    bool Test(IEnumerable<string> source, string value) => source.Contains(value);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task List_ContainsWithComparer_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System;
                using System.Collections.Generic;
                using System.Linq;

                class Sample
                {
                    bool Test(List<string> list, string value) => list.Contains(value, StringComparer.Ordinal);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task HashSet_AssignedToIEnumerable_Contains_NoDiagnostic()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Collections.Generic;
                using System.Linq;

                class Sample
                {
                    bool Test(string value)
                    {
                        IEnumerable<string> source = new HashSet<string>();
                        return source.Contains(value);
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task HashSet_ContainsWithComparer()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System;
                using System.Collections.Generic;
                using System.Linq;

                class Sample
                {
                    bool Test(HashSet<string> set, string value) => [|set.Contains(value, StringComparer.Ordinal)|];
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task HashSet_EnumerableContainsWithComparer()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System;
                using System.Collections.Generic;
                using System.Linq;

                class Sample
                {
                    bool Test(HashSet<string> set, string value) => [|Enumerable.Contains(set, value, StringComparer.Ordinal)|];
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task HashSet_AssignedToIEnumerable_ContainsWithComparer()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System;
                using System.Collections.Generic;
                using System.Linq;

                class Sample
                {
                    bool Test(string value)
                    {
                        IEnumerable<string> source = new HashSet<string>();
                        return [|source.Contains(value, StringComparer.Ordinal)|];
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task HashSet_ContainsObject()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Collections.Generic;
                using System.Linq;

                class Sample
                {
                    bool Test(HashSet<string> set, object value) => [|set.Contains(value)|];
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task HashSet_UsedAsCovariantEnumerable_Contains()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System.Collections.Generic;
                using System.Linq;

                class Sample
                {
                    bool Test(object value)
                    {
                        IEnumerable<object> source = new HashSet<string>();
                        return [|source.Contains(value)|];
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task SortedSet_ContainsWithComparer()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System;
                using System.Collections.Generic;
                using System.Linq;

                class Sample
                {
                    bool Test(SortedSet<string> set, string value) => [|set.Contains(value, StringComparer.Ordinal)|];
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task ImmutableHashSet_ContainsWithComparer()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System;
                using System.Collections.Immutable;
                using System.Linq;

                class Sample
                {
                    bool Test(ImmutableHashSet<string> set, string value) => [|set.Contains(value, StringComparer.Ordinal)|];
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task FrozenSet_ContainsWithComparer()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System;
                using System.Collections.Frozen;
                using System.Linq;

                class Sample
                {
                    bool Test(FrozenSet<string> set, string value) => [|set.Contains(value, StringComparer.Ordinal)|];
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task IReadOnlySet_ContainsWithComparer()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System;
                using System.Collections.Generic;
                using System.Linq;

                class Sample
                {
                    bool Test(IReadOnlySet<string> set, string value) => [|set.Contains(value, StringComparer.Ordinal)|];
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task ISet_ContainsWithComparer()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System;
                using System.Collections.Generic;
                using System.Linq;

                class Sample
                {
                    bool Test(ISet<string> set, string value) => [|set.Contains(value, StringComparer.Ordinal)|];
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task CustomSet_ContainsWithComparer()
    {
        return new AnalyzerTest
        {
            TestCode = """
                using System;
                using System.Collections.Generic;
                using System.Linq;

                class Sample
                {
                    bool Test(CustomSet set, string value) => [|set.Contains(value, StringComparer.Ordinal)|];
                }

                class CustomSet : HashSet<string>
                {
                }
                """,
        }.RunAsync();
    }
}

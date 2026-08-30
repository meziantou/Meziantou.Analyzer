using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseDefaultEqualsOnValueTypeAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseDefaultEqualsOnValueTypeAnalyzer_HashSetTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.DisabledDiagnostics.Add(RuleIdentifiers.DoNotUseDefaultEqualsOnValueType);
        return test;
    }

    [Theory]
    [InlineData("new System.Collections.Generic.HashSet<Test>()")]
    [InlineData("new System.Collections.Generic.Dictionary<Test, object>()")]
    [InlineData("new System.Collections.Concurrent.ConcurrentDictionary<Test, object>()")]
    [InlineData("System.Collections.Immutable.ImmutableHashSet.Create<Test>()")]
    [InlineData("System.Collections.Immutable.ImmutableDictionary.Create<Test, object>()")]
    [InlineData("System.Collections.Immutable.ImmutableSortedDictionary.Create<Test, object>()")]
    [InlineData("System.Collections.Immutable.ImmutableHashSet<Test>.Empty")]
    [InlineData("System.Collections.Immutable.ImmutableDictionary<Test, object>.Empty")]
    [InlineData("System.Collections.Immutable.ImmutableSortedDictionary<Test, object>.Empty")]
    public Task Constructor_DefaultImplementation(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            struct Test
            {
                void A()
                {
                    var collection = {|MA0066:{{text}}|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("System.Collections.Immutable.ImmutableHashSet<Test>.Empty")]
    public Task Empty_WithComparer(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            struct Test
            {
                void A()
                {
                    var collection = {{text}}.WithComparer(default);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("System.Collections.Immutable.ImmutableDictionary<Test, object>.Empty")]
    [InlineData("System.Collections.Immutable.ImmutableSortedDictionary<Test, object>.Empty")]
    public Task Empty_WithComparers(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            struct Test
            {
                void A()
                {
                    var collection = {{text}}.WithComparers(default);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("new System.Collections.Generic.HashSet<Test>()")]
    [InlineData("new System.Collections.Generic.Dictionary<Test, object>()")]
    [InlineData("new System.Collections.Concurrent.ConcurrentDictionary<Test, object>()")]
    [InlineData("System.Collections.Immutable.ImmutableHashSet.Create<Test>()")]
    [InlineData("System.Collections.Immutable.ImmutableDictionary.Create<Test, object>()")]
    [InlineData("System.Collections.Immutable.ImmutableSortedDictionary.Create<Test, object>()")]
    [InlineData("System.Collections.Immutable.ImmutableHashSet<Test>.Empty")]
    [InlineData("System.Collections.Immutable.ImmutableDictionary<Test, object>.Empty")]
    [InlineData("System.Collections.Immutable.ImmutableSortedDictionary<Test, object>.Empty")]
    public Task Constructor_EqualsOverriden(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            struct Test
            {
                public override bool Equals(object o) => throw null;
                public override int GetHashCode() => throw null;

                void A()
                {
                    _ = {{text}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("new System.Collections.Generic.HashSet<Test>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
    [InlineData("new System.Collections.Generic.Dictionary<Test, object>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
    [InlineData("new System.Collections.Concurrent.ConcurrentDictionary<Test, object>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
    [InlineData("System.Collections.Immutable.ImmutableHashSet.Create<Test>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
    [InlineData("System.Collections.Immutable.ImmutableDictionary.Create<Test, object>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
    [InlineData("System.Collections.Immutable.ImmutableSortedDictionary.Create<Test, object>(null, System.Collections.Generic.EqualityComparer<object>.Default)")]
    public Task Constructor_EqualityComparer(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            struct Test
            {
                void A()
                {
                    _ = {{text}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("new System.Collections.Generic.HashSet<Test>()")]
    [InlineData("new System.Collections.Generic.Dictionary<Test, object>()")]
    [InlineData("new System.Collections.Concurrent.ConcurrentDictionary<Test, object>()")]
    [InlineData("System.Collections.Immutable.ImmutableHashSet.Create<Test>()")]
    [InlineData("System.Collections.Immutable.ImmutableDictionary.Create<Test, object>()")]
    [InlineData("System.Collections.Immutable.ImmutableSortedDictionary.Create<Test, object>()")]
    public Task GetHashCode_Enum(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""

            enum Test
            {
                A,
                B,
            }

            class Sample
            {
                public void A()
                {
                    _ = {{text}};
                }
            }
            """;

        return test.RunAsync();
    }
}

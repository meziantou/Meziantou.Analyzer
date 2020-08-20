using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DoNotUseDefaultEqualsOnValueTypeAnalyzer_HashSetTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseDefaultEqualsOnValueTypeAnalyzer>(id: "MA0066");
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
        public async Task Constructor_DefaultImplementation(string text)
        {
            var sourceCode = @"
struct Test
{
    void A()
    {
        var collection = [||]" + text + @";
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
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
        public async Task Constructor_EqualsOverriden(string text)
        {
            var sourceCode = @"
struct Test
{
    public override bool Equals(object o) => throw null;
    public override int GetHashCode() => throw null;

    void A()
    {
        _ = " + text + @";
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("new System.Collections.Generic.HashSet<Test>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
        [InlineData("new System.Collections.Generic.Dictionary<Test, object>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
        [InlineData("new System.Collections.Concurrent.ConcurrentDictionary<Test, object>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
        [InlineData("System.Collections.Immutable.ImmutableHashSet.Create<Test>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
        [InlineData("System.Collections.Immutable.ImmutableDictionary.Create<Test, object>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
        [InlineData("System.Collections.Immutable.ImmutableSortedDictionary.Create<Test, object>(null, System.Collections.Generic.EqualityComparer<object>.Default)")]
        public async Task Constructor_EqualityComparer(string text)
        {
            var sourceCode = @"
struct Test
{
    void A()
    {
        _ = " + text + @";
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("new System.Collections.Generic.HashSet<Test>()")]
        [InlineData("new System.Collections.Generic.Dictionary<Test, object>()")]
        [InlineData("new System.Collections.Concurrent.ConcurrentDictionary<Test, object>()")]
        [InlineData("System.Collections.Immutable.ImmutableHashSet.Create<Test>()")]
        [InlineData("System.Collections.Immutable.ImmutableDictionary.Create<Test, object>()")]
        [InlineData("System.Collections.Immutable.ImmutableSortedDictionary.Create<Test, object>()")]
        public async Task GetHashCode_Enum(string text)
        {
            string sourceCode = @"
enum Test
{
    A,
    B,
}

class Sample
{
    public void A()
    {
        _ = " + text + @";
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }


    }
}

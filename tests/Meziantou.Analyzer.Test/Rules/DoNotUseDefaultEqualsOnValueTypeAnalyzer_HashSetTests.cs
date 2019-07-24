using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class DoNotUseDefaultEqualsOnValueTypeAnalyzer_HashSetTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseDefaultEqualsOnValueTypeAnalyzer>(id: "MA0066");
        }

        [DataTestMethod]
        [DataRow("new System.Collections.Generic.HashSet<Test>()")]
        [DataRow("new System.Collections.Generic.Dictionary<Test, object>()")]
        [DataRow("new System.Collections.Concurrent.ConcurrentDictionary<Test, object>()")]
        [DataRow("System.Collections.Immutable.ImmutableHashSet.Create<Test>()")]
        [DataRow("System.Collections.Immutable.ImmutableDictionary.Create<Test, object>()")]
        [DataRow("System.Collections.Immutable.ImmutableSortedDictionary.Create<Test, object>()")]
        [DataRow("System.Collections.Immutable.ImmutableHashSet<Test>.Empty")]
        [DataRow("System.Collections.Immutable.ImmutableDictionary<Test, object>.Empty")]
        [DataRow("System.Collections.Immutable.ImmutableSortedDictionary<Test, object>.Empty")]
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

        [DataTestMethod]
        [DataRow("new System.Collections.Generic.HashSet<Test>()")]
        [DataRow("new System.Collections.Generic.Dictionary<Test, object>()")]
        [DataRow("new System.Collections.Concurrent.ConcurrentDictionary<Test, object>()")]
        [DataRow("System.Collections.Immutable.ImmutableHashSet.Create<Test>()")]
        [DataRow("System.Collections.Immutable.ImmutableDictionary.Create<Test, object>()")]
        [DataRow("System.Collections.Immutable.ImmutableSortedDictionary.Create<Test, object>()")]
        [DataRow("System.Collections.Immutable.ImmutableHashSet<Test>.Empty")]
        [DataRow("System.Collections.Immutable.ImmutableDictionary<Test, object>.Empty")]
        [DataRow("System.Collections.Immutable.ImmutableSortedDictionary<Test, object>.Empty")]
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

        [DataTestMethod]
        [DataRow("new System.Collections.Generic.HashSet<Test>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
        [DataRow("new System.Collections.Generic.Dictionary<Test, object>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
        [DataRow("new System.Collections.Concurrent.ConcurrentDictionary<Test, object>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
        [DataRow("System.Collections.Immutable.ImmutableHashSet.Create<Test>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
        [DataRow("System.Collections.Immutable.ImmutableDictionary.Create<Test, object>(System.Collections.Generic.EqualityComparer<Test>.Default)")]
        [DataRow("System.Collections.Immutable.ImmutableSortedDictionary.Create<Test, object>(null, System.Collections.Generic.EqualityComparer<object>.Default)")]
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
    }
}

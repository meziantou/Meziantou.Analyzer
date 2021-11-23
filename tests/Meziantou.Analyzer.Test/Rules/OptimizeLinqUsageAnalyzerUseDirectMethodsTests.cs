using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerUseDirectMethodsTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods)
            .WithCodeFixProvider<OptimizeLinqUsageFixer>();
    }

    [Fact]
    public async Task FirstOrDefaultAsync()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        var list = new System.Collections.Generic.List<int>();
        list.FirstOrDefault();
        list.[|FirstOrDefault|](x => x == 0);
        enumerable.FirstOrDefault();
        enumerable.FirstOrDefault(x => x == 0);
    }
}
";
        const string CodeFix = @"using System.Linq;
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
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use 'Find()' instead of 'FirstOrDefault()'")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Count_IEnumerableAsync()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable.Count();
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Count_ListAsync()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        _ = list.[|Count|]();
        list.Count(x => x == 0);
    }
}
";

        const string CodeFix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        _ = list.Count;
        list.Count(x => x == 0);
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use 'Count' instead of 'Count()'")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Count_ICollectionExplicitImplementationAsync()
    {
        const string SourceCode = @"
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
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Count_ArrayAsync()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[10];
        _ = list.[|Count|]();
        list.Count(x => x == 0);
    }
}
";

        const string Fix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[10];
        _ = list.Length;
        list.Count(x => x == 0);
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use 'Length' instead of 'Count()'")
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }
}

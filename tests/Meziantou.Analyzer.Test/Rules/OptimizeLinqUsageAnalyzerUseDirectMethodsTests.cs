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
    public Task FirstOrDefaultAsync_Net9()
        => CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode("""
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
                """)
              .ValidateAsync();

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
    public async Task FirstOrDefaultAsync_Cast()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        System.Func<int, bool> predicate = _ => true;
        list.FirstOrDefault(predicate);
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_Cast_ConfigureEnabled()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        System.Func<int, bool> predicate = _ => true;
        list.[|FirstOrDefault|](predicate);
    }
}
";
        const string CodeFix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        System.Func<int, bool> predicate = _ => true;
        list.Find(new System.Predicate<int>(predicate));
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0020.report_when_conversion_needed", "true")
              .ShouldReportDiagnosticWithMessage("Use 'Find()' instead of 'FirstOrDefault()'")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task TrueForAll()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        var list = new System.Collections.Generic.List<int>();
        list.[|All|](x => x == 0);
        enumerable.All(x => x == 0);
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
        list.TrueForAll(x => x == 0);
        enumerable.All(x => x == 0);
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use 'TrueForAll()' instead of 'All()'")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task TrueForAll_Cast()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        System.Func<int, bool> predicate = _ => true;
        list.All(predicate);
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task TrueForAll_Cast_ConfigureEnabled()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        System.Func<int, bool> predicate = _ => true;
        list.[|All|](predicate);
    }
}
";
        const string CodeFix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        System.Func<int, bool> predicate = _ => true;
        list.TrueForAll(new System.Predicate<int>(predicate));
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0020.report_when_conversion_needed", "true")
              .ShouldReportDiagnosticWithMessage("Use 'TrueForAll()' instead of 'All()'")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Exists()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        var list = new System.Collections.Generic.List<int>();
        list.[|Any|](x => x == 0);
        enumerable.Any(x => x == 0);
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
        list.Exists(x => x == 0);
        enumerable.Any(x => x == 0);
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use 'Exists()' instead of 'Any()'")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Exists_Cast()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        System.Func<int, bool> predicate = _ => true;
        list.Any(predicate);
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Exists_Cast_ConfigureEnabled()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        System.Func<int, bool> predicate = _ => true;
        list.[|Any|](predicate);
    }
}
";
        const string CodeFix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        System.Func<int, bool> predicate = _ => true;
        list.Exists(new System.Predicate<int>(predicate));
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0020.report_when_conversion_needed", "true")
              .ShouldReportDiagnosticWithMessage("Use 'Exists()' instead of 'Any()'")
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

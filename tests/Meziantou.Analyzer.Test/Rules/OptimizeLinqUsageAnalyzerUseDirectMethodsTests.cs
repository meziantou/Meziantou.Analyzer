using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class OptimizeLinqUsageAnalyzerUseDirectMethodsTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: "MA0020")
                .WithCodeFixProvider<OptimizeLinqUsageFixer>();
        }

        [TestMethod]
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
        list.FirstOrDefault(x => x == 0); // Error
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
        list.Find(x => x == 0); // Error
        enumerable.FirstOrDefault();
        enumerable.FirstOrDefault(x => x == 0);
    }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 9, column: 9, message: "Use 'Find()' instead of 'FirstOrDefault()'")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
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

        [TestMethod]
        public async Task Count_ListAsync()
        {
            const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        _ = list.Count();
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
                  .ShouldReportDiagnostic(line: 7, column: 13, message: "Use 'Count' instead of 'Count()'")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
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

        [TestMethod]
        public async Task Count_ArrayAsync()
        {
            const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[10];
        _ = list.Count();
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
                  .ShouldReportDiagnostic(line: 7, column: 13, message: "Use 'Length' instead of 'Count()'")
                  .ShouldFixCodeWith(Fix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task ElementAt_ListAsync()
        {
            const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        _ = list.ElementAt(10);
        list.ElementAtOrDefault(10);
    }
}
";
            const string CodeFix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        _ = list[10];
        list.ElementAtOrDefault(10);
    }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 13, message: "Use '[]' instead of 'ElementAt()'")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task ElementAt_ArrayAsync()
        {
            const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        _ = list.ElementAt(10);
        list.ElementAtOrDefault(10);
    }
}
";
            const string CodeFix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        _ = list[10];
        list.ElementAtOrDefault(10);
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 13, message: "Use '[]' instead of 'ElementAt()'")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task First_ArrayAsync()
        {
            const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        _ = list.First();
        list.First(x=> x == 0);
    }
}
";
            const string CodeFix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        _ = list[0];
        list.First(x=> x == 0);
    }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 13, message: "Use '[]' instead of 'First()'")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Last_Array()
        {
            const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        _ = list.Last();
        list.First(x=> x == 0);
    }
}
";
            const string CodeFix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        _ = list[list.Length - 1];
        list.First(x=> x == 0);
    }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 13, message: "Use '[]' instead of 'Last()'")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Last_List()
        {
            const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        _ = list.Last();
        list.First(x=> x == 0);
    }
}
";
            const string CodeFix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        _ = list[list.Count - 1];
        list.First(x=> x == 0);
    }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 13, message: "Use '[]' instead of 'Last()'")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }
    }
}

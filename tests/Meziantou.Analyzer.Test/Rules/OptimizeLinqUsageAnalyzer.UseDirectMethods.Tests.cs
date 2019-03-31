using System.Collections.Generic;
using System.Linq;
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
                .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: "MA0020");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task FirstOrDefaultAsync()
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
            await CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 9, column: 9, message: "Use 'Find()' instead of 'FirstOrDefault()'")
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Count_IEnumerableAsync()
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
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(SourceCode)
                  .ShouldNotReportDiagnostic()
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Count_ListAsync()
        {
            const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        list.Count();
        list.Count(x => x == 0);
    }
}
";
            await CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 9, message: "Use 'Count' instead of 'Count()'")
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Count_ICollectionExplicitImplementationAsync()
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
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(SourceCode)
                  .ShouldNotReportDiagnostic()
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Count_ArrayAsync()
        {
            const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[10];
        list.Count();
        list.Count(x => x == 0);
    }
}
";
            await CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 9, message: "Use 'Length' instead of 'Count()'")
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task ElementAt_ListAsync()
        {
            const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        list.ElementAt(10);
        list.ElementAtOrDefault(10);
    }
}
";
            await CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 9, message: "Use '[]' instead of 'ElementAt()'")
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task ElementAt_ArrayAsync()
        {
            const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        list.ElementAt(10);
        list.ElementAtOrDefault(10);
    }
}
";
            await CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 9, message: "Use '[]' instead of 'ElementAt()'")
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task First_ArrayAsync()
        {
            const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        list.First();
        list.First(x=> x == 0);
    }
}
";
            await CreateProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 9, message: "Use '[]' instead of 'First()'")
                  .ValidateAsync();
        }
    }
}

﻿using System.Collections.Generic;
using System.Linq;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class OptimizeLinqUsageAnalyzerUseDirectMethodsTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new OptimizeLinqUsageAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0020";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Info;

        [TestMethod]
        public void FirstOrDefault()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
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
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 9, column: 9, message: "Use 'Find()' instead of 'FirstOrDefault()'"));
        }

        [TestMethod]
        public void Count_IEnumerable()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        enumerable.Count();
    }
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Count_List()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        list.Count();
        list.Count(x => x == 0);
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 9, message: "Use 'Count' instead of 'Count()'"));
        }

        [TestMethod]
        public void Count_ICollectionExplicitImplementation()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"
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
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Count_Array()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[10];
        list.Count();
        list.Count(x => x == 0);
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 9, message: "Use 'Length' instead of 'Count()'"));
        }

        [TestMethod]
        public void ElementAt_List()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        list.ElementAt(10);
        list.ElementAtOrDefault(10);
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 9, message: "Use '[]' instead of 'ElementAt()'"));
        }

        [TestMethod]
        public void ElementAt_Array()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        list.ElementAt(10);
        list.ElementAtOrDefault(10);
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 9, message: "Use '[]' instead of 'ElementAt()'"));
        }

        [TestMethod]
        public void First_Array()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        list.First();
        list.First(x=> x == 0);
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 9, message: "Use '[]' instead of 'First()'"));
        }
    }
}

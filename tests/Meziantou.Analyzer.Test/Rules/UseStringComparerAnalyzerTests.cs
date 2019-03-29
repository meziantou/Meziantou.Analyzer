using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class UseStringComparerAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseStringComparerAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new UseStringComparerFixer();
        protected override string ExpectedDiagnosticId => "MA0002";
        protected override string ExpectedDiagnosticMessage => "Use an overload that has a IEqualityComparer<string> parameter";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void HashSet_Int32_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(HashSet<>))
                  .WithSourceCode(@"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<int>();
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void HashSet_String_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(HashSet<>))
                  .WithSourceCode(@"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<string>();
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 9);
            VerifyDiagnostic(project, expected);

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
    }
}";
            VerifyFix(project, fixtest);
        }

        [TestMethod]
        public void HashSet_String_StringEqualityComparer_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(HashSet<>))
                  .WithSourceCode(@"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Dictionary_String_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.Dictionary<string, int>();
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 9);
            VerifyDiagnostic(project, expected);

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
    }
}";
            VerifyFix(project, fixtest);
        }

        [TestMethod]
        public void ConcurrentDictionary_String_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(ConcurrentDictionary<,>))
                  .WithSourceCode(@"
class TypeName
{
    public void Test()
    {
        new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 9);
            VerifyDiagnostic(project, expected);

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Concurrent.ConcurrentDictionary<string, int>(System.StringComparer.Ordinal);
    }
}";
            VerifyFix(project, fixtest);
        }

        [TestMethod]
        public void EnumerableContains_String_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Enumerable))
                  .AddReference(typeof(IEnumerable<>))
                  .WithSourceCode(@"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.Contains("""");
    }
}");

            var expected = CreateDiagnosticResult(line: 7, column: 9);
            VerifyDiagnostic(project, expected);

            var fixtest = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.Contains("""", System.StringComparer.Ordinal);
    }
}";
            VerifyFix(project, fixtest);
        }

        [TestMethod]
        public void EnumerableToDictionary_String_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Dictionary<,>))
                  .AddReference(typeof(Enumerable))
                  .AddReference(typeof(IEnumerable<>))
                  .WithSourceCode(@"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.ToDictionary(p => p);
    }
}");

            var expected = CreateDiagnosticResult(line: 7, column: 9);
            VerifyDiagnostic(project, expected);

            var fixtest = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.ToDictionary(p => p, System.StringComparer.Ordinal);
    }
}";
            VerifyFix(project, fixtest);
        }
    }
}

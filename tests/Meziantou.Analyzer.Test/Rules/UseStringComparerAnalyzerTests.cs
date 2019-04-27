using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class UseStringComparerAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseStringComparerAnalyzer>()
                .WithCodeFixProvider<UseStringComparerFixer>();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task HashSet_Int32_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<int>();
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task HashSet_String_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<string>();
    }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 9)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task HashSet_String_StringEqualityComparer_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Dictionary_String_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.Dictionary<string, int>();
    }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 9)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task ConcurrentDictionary_String_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
    }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        new System.Collections.Concurrent.ConcurrentDictionary<string, int>(System.StringComparer.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 9)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task EnumerableContains_String_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.Contains("""");
    }
}";
            const string CodeFix = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.Contains("""", System.StringComparer.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 9)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task EnumerableToDictionary_String_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.ToDictionary(p => p);
    }
}";
            const string CodeFix = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        System.Collections.Generic.IEnumerable<string> obj = null;
        obj.ToDictionary(p => p, System.StringComparer.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 9)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }
    }
}

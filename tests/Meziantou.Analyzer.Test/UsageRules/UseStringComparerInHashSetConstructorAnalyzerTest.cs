using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test
{
    [TestClass]
    public class UseStringComparerInHashSetConstructorAnalyzerTest : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseStringComparerInHashSetConstructorAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new UseStringComparerInHashSetConstructorFixer();
        protected override string ExpectedDiagnosticId => "MA0002";
        protected override string ExpectedDiagnosticMessage => "Use an overload of the constructor that has a IEqualityComparer<string> parameter";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void EmptyString_ShouldNotReportDiagnosticForEmptyString()
        {
            var project = new ProjectBuilder();
            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void HashSet_Int32_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(HashSet<>))
                  .WithSource(@"
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
                  .WithSource(@"
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
                  .WithSource(@"
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
                  .WithSource(@"
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
                  .AddConcurrentDictionaryApi()
                  .WithSource(@"
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
    }
}

using Meziantou.Analyzer.UsageRules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.UsageRules
{
    [TestClass]
    public class UseStringComparisonAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseStringComparisonAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new UseStringComparisonFixer();
        protected override string ExpectedDiagnosticId => "MA0001";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void Equals_ShouldNotReportDiagnosticForEmptyString()
        {
            var project = new ProjectBuilder();
            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Equals_String_string_StringComparison_ShouldNotReportDiagnosticWhenStringComparisonIsSpecified()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TypeName
{
    public void Test()
    {
        var a = ""test"";
        string.Equals(a, ""v"", System.StringComparison.Ordinal);
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Equals_String_string_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TypeName
{
    public void Test()
    {
        System.String.Equals(""a"", ""v"");
    }
}");
            var expected = CreateDiagnosticResult(line: 6, column: 9, message: "Use an overload of 'Equals' that has a StringComparison parameter");
            VerifyDiagnostic(project, expected);

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        System.String.Equals(""a"", ""v"", System.StringComparison.Ordinal);
    }
}";
            VerifyFix(project, fixtest);
        }

        [TestMethod]
        public void Equals_String_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TypeName
{
    public void Test()
    {
        ""a"".Equals(""v"");
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 9, message: "Use an overload of 'Equals' that has a StringComparison parameter");
            VerifyDiagnostic(project, expected);

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        ""a"".Equals(""v"", System.StringComparison.Ordinal);
    }
}";
            VerifyFix(project, fixtest);
        }

        [TestMethod]
        public void IndexOf_Char_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TypeName
{
    public void Test()
    {
        ""a"".IndexOf('v');
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 9, message: "Use an overload of 'IndexOf' that has a StringComparison parameter");
            VerifyDiagnostic(project, expected);

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        ""a"".IndexOf('v', System.StringComparison.Ordinal);
    }
}";
            VerifyFix(project, fixtest);
        }

        [TestMethod]
        public void IndexOf_String_StringComparison_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TypeName
{
    public void Test()
    {
        ""a"".IndexOf(""v"", System.StringComparison.Ordinal);
    }
}");
            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void IndexOf_String_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TypeName
{
    public void Test()
    {
        ""a"".IndexOf(""v"");
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 9, message: "Use an overload of 'IndexOf' that has a StringComparison parameter");
            VerifyDiagnostic(project, expected);

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        ""a"".IndexOf(""v"", System.StringComparison.Ordinal);
    }
}";
            VerifyFix(project, fixtest);
        }

        [TestMethod]
        public void StartsWith_String_StringComparison_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TypeName
{
    public void Test()
    {
        ""a"".StartsWith(""v"", System.StringComparison.Ordinal);
    }
}");
            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void StartsWith_String_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
class TypeName
{
    public void Test()
    {
        ""a"".StartsWith(""v"");
    }
}");
            var expected = CreateDiagnosticResult(line: 6, column: 9, message: "Use an overload of 'StartsWith' that has a StringComparison parameter");
            VerifyDiagnostic(project, expected);

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        ""a"".StartsWith(""v"", System.StringComparison.Ordinal);
    }
}";
            VerifyFix(project, fixtest);
        }
    }
}

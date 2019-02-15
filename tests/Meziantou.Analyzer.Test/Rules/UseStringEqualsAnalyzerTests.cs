using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class UseStringEqualsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseStringEqualsAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new UseStringEqualsFixer();
        protected override string ExpectedDiagnosticId => "MA0006";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void Equals_ShouldNotReportDiagnosticForEmptyString()
        {
            var project = new ProjectBuilder();
            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Equals_StringLiteral_stringLiteral_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                .WithSource(@"
class TypeName
{
    public void Test()
    {
        var a = ""a"" == ""v"";
    }
}");
            var expected = CreateDiagnosticResult(line: 6, column: 17, message: "Use string.Equals instead of Equals operator");
            VerifyDiagnostic(project, expected);

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        var a = string.Equals(""a"", ""v"", System.StringComparison.Ordinal);
    }
}";
            VerifyFix(project, fixtest);
        }

        [TestMethod]
        public void NotEquals_StringLiteral_stringLiteral_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
    .WithSource(@"
class TypeName
{
    public void Test()
    {
        var a = ""a"" != ""v"";
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 17, message: "Use string.Equals instead of NotEquals operator");
            VerifyDiagnostic(project, expected);

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        var a = !string.Equals(""a"", ""v"", System.StringComparison.Ordinal);
    }
}";
            VerifyFix(project, fixtest);
        }

        [TestMethod]
        public void Equals_StringVariable_stringLiteral_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
    .WithSource(@"
class TypeName
{
    public void Test()
    {
        string str = """";
        var a = str == ""v"";
    }
}");

            var expected = CreateDiagnosticResult(line: 7, column: 17, message: "Use string.Equals instead of Equals operator");
            VerifyDiagnostic(project, expected);

            var fixtest = @"
class TypeName
{
    public void Test()
    {
        string str = """";
        var a = string.Equals(str, ""v"", System.StringComparison.Ordinal);
    }
}";
            VerifyFix(project, fixtest);
        }

        [TestMethod]
        public void Equals_ObjectVariable_stringLiteral_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
    .WithSource(@"
class TypeName
{
    public void Test()
    {
        object str = """";
        var a = str == ""v"";
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void Equals_stringLiteral_null_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
    .WithSource(@"
class TypeName
{
    public void Test()
    {
        var a = """" == null;
        var b = null == """";
    }
}");

            VerifyDiagnostic(project);
        }
    }
}

using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class OptimizeStringBuilderUsageAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new OptimizeStringBuilderUsageAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0028";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void AppendFormat_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendFormat(""{10}"", 10);
    }
}");

            VerifyDiagnostic(project);
        }

        [DataTestMethod]
        [DataRow("10")]
        [DataRow("10 + 20")]
        [DataRow(@"""abc""")]
        [DataRow(@"$""abc""")]
        public void Append_NoDiagnostic(string text)
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append(" + text + @");
    }
}");

            VerifyDiagnostic(project);
        }

        [DataTestMethod]
        [DataRow(@"$""a{1}""")]
        [DataRow(@"""a"" + 10")]
        [DataRow(@"10 + 20 + ""a""")]
        public void Append_ReportDiagnostic(string text)
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append(" + text + @");
    }
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 9));
        }

        [DataTestMethod]
        [DataRow(@"""abc""")]
        [DataRow(@"$""abc""")]
        public void AppendLine_NoDiagnostic(string text)
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendLine(" + text + @");
    }
}");

            VerifyDiagnostic(project);
        }

        [DataTestMethod]
        [DataRow(@"$""a{1}""")]
        [DataRow(@"""a"" + 10")]
        [DataRow(@"10 + 20 + ""a""")]
        [DataRow(@"10.ToString()")]
        public void AppendLine_ReportDiagnostic(string text)
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendLine(" + text + @");
    }
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 9));
        }

        [DataTestMethod]
        [DataRow(@"""abc""")]
        [DataRow(@"$""abc""")]
        public void Insert_NoDiagnostic(string text)
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Insert(0, " + text + @");
    }
}");

            VerifyDiagnostic(project);
        }

        [DataTestMethod]
        [DataRow(@"$""a{1}""")]
        [DataRow(@"""a"" + 10")]
        [DataRow(@"10 + 20 + ""a""")]
        [DataRow(@"10.ToString()")]
        public void Insert_ReportDiagnostic(string text)
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Insert(0, " + text + @");
    }
}");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 6, column: 9));
        }
    }
}

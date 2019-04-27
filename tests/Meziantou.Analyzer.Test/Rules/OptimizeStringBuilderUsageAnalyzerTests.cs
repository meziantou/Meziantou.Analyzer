using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class OptimizeStringBuilderUsageAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<OptimizeStringBuilderUsageAnalyzer>(id: "MA0028");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task AppendFormat_NoDiagnosticAsync()
        {
            const string SourceCode = @"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendFormat(""{10}"", 10);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("10")]
        [DataRow("10 + 20")]
        [DataRow(@"""abc""")]
        [DataRow(@"$""abc""")]
        [DataRow(@"$""abc{""test""}""")]
        [DataRow(@"""abc"" + ""test""")]
        [DataRow(@"$""abc{""test""}"" + ""test""")]
        public async System.Threading.Tasks.Task Append_NoDiagnosticAsync(string text)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append(" + text + @");
    }
}")
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow(@"$""a{1}""")]
        [DataRow(@"""a"" + 10")]
        [DataRow(@"10 + 20 + ""a""")]
        [DataRow(@"""""")]
        [DataRow(@""""" + """"")]
        [DataRow(@""""".Substring(0, 10)")]
        public async System.Threading.Tasks.Task Append_ReportDiagnosticAsync(string text)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append(" + text + @");
    }
}")
                  .ShouldReportDiagnostic(line: 6, column: 9)
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow(@"""abc""")]
        [DataRow(@"$""abc""")]
        public async System.Threading.Tasks.Task AppendLine_NoDiagnosticAsync(string text)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendLine(" + text + @");
    }
}")
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow(@"$""a{1}""")]
        [DataRow(@"""a"" + 10")]
        [DataRow(@"10 + 20 + ""a""")]
        [DataRow(@"10.ToString()")]
        public async System.Threading.Tasks.Task AppendLine_ReportDiagnosticAsync(string text)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendLine(" + text + @");
    }
}")
                  .ShouldReportDiagnostic(line: 6, column: 9)
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow(@"""abc""")]
        [DataRow(@"$""abc""")]
        public async System.Threading.Tasks.Task Insert_NoDiagnosticAsync(string text)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Insert(0, " + text + @");
    }
}")
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow(@"$""a{1}""")]
        [DataRow(@"""a"" + 10")]
        [DataRow(@"10 + 20 + ""a""")]
        [DataRow(@"10.ToString()")]
        public async System.Threading.Tasks.Task Insert_ReportDiagnosticAsync(string text)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Insert(0, " + text + @");
    }
}")
                  .ShouldReportDiagnostic(line: 6, column: 9)
                  .ValidateAsync();
        }
    }
}

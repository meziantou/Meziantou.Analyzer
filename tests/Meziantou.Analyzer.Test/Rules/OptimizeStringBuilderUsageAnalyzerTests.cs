using System.Collections.Generic;
using System.Threading.Tasks;
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
                .WithAnalyzer<OptimizeStringBuilderUsageAnalyzer>()
                .WithCodeFixProvider<OptimizeStringBuilderUsageFixer>();
        }

        [TestMethod]
        public async Task AppendFormat_NoDiagnostic()
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
        public async Task Append_NoDiagnostic(string text)
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
        public async Task Append_ReportDiagnostic(string text)
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
        public async Task AppendLine_NoDiagnostic(string text)
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
        public async Task AppendLine_ReportDiagnostic(string text)
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
        public async Task Insert_NoDiagnostic(string text)
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
        public async Task Insert_ReportDiagnostic(string text)
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

        private static IEnumerable<object[]> EmptyStringsArguments
        {
            get
            {
                yield return new object[] { @"$""""" };
                yield return new object[] { @"$""{""""}""" };
                yield return new object[] { @"""""" };
                yield return new object[] { @""""" + """"" };
                yield return new object[] { @"string.Empty" };
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(EmptyStringsArguments), DynamicDataSourceType.Property)]
        public async Task AppendLine_EmptyString(string text)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [|]new StringBuilder().AppendLine(" + text + @");
    }
}")
                  .ShouldReportDiagnostic()
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendLine();
    }
}")
                  .ValidateAsync();
        }
        
        [DataTestMethod]
        [DynamicData(nameof(EmptyStringsArguments), DynamicDataSourceType.Property)]
        public async Task Append_EmptyString(string text)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [|]new StringBuilder().Append(" + text + @").AppendLine();
    }
}")
                  .ShouldReportDiagnostic()
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendLine();
    }
}")
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DynamicData(nameof(EmptyStringsArguments), DynamicDataSourceType.Property)]
        public async Task Insert_EmptyString(string text)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [|]new StringBuilder().Insert(0, " + text + @").AppendLine();
    }
}")
                  .ShouldReportDiagnostic()
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendLine();
    }
}")
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow(@"""a""")]
        public async Task Append_OneCharString(string text)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append([|]" + text + @");
    }
}")
                  .ShouldReportDiagnostic()
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append('a');
    }
}")
                  .ValidateAsync();
        }
    }
}

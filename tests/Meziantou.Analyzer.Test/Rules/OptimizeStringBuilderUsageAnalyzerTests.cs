using System.Collections.Generic;
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class OptimizeStringBuilderUsageAnalyzerTests
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
        [DataRow(@"$""a{1}""")]
        [DataRow(@"""a"" + 10")]
        [DataRow(@"10 + 20 + ""a""")]
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
        [DataRow(@"10.ToString()")]
        public async Task Insert_Diagnostic(string text)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [|]new StringBuilder().Insert(0, " + text + @");
    }
}")
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

        [DataTestMethod]
        [DataRow(@"""a""")]
        public async Task Insert_OneCharString(string text)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Insert(0, [|]" + text + @");
    }
}")
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Insert(0, 'a');
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Append_InterpolatedString()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [|]new StringBuilder().Append($""A{1}BC{2:X2}DEF{1,-2:N2}"");
    }
}")
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append('A').Append(1).Append(""BC"").AppendFormat(""{0:X2}"", 2).Append(""DEF"").AppendFormat(""{0,-2:N2}"", 1);
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task AppendLine_InterpolatedString_FinishWithString()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [|]new StringBuilder().AppendLine($""A{1}BC{2:X2}DEF"");
    }
}")
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append('A').Append(1).Append(""BC"").AppendFormat(""{0:X2}"", 2).AppendLine(""DEF"");
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task AppendLine_InterpolatedString_FinishWithObject()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [|]new StringBuilder().AppendLine($""A{1}BC{2:X2}"");
    }
}")
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append('A').Append(1).Append(""BC"").AppendFormat(""{0:X2}"", 2).AppendLine();
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Append_StringAdd()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        var a = """";
        [|]new StringBuilder().Append(""ab"" + a);
    }
}")
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        var a = """";
        new StringBuilder().Append(""ab"").Append(a);
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task AppendLine_StringAdd()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        var a = """";
        [|]new StringBuilder().AppendLine(""ab"" + a);
    }
}")
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        var a = """";
        new StringBuilder().Append(""ab"").AppendLine(a);
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Append_ToString()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [|]new StringBuilder().Append(1.ToString());
    }
}")
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append(1);
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task AppendLine_ToString()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [|]new StringBuilder().AppendLine(1.ToString());
    }
}")
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append(1).AppendLine();
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Append_AppendFormat()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [|]new StringBuilder().Append(1.ToString(""{0}"", null));
    }
}")
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendFormat(null, ""{0}"", 1);
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task AppendLine_AppendFormat()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [|]new StringBuilder().AppendLine(1.ToString(""{0}"", null));
    }
}")
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendFormat(null, ""{0}"", 1).AppendLine();
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task AppendLine_AppendSubString()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [|]new StringBuilder().AppendLine("""".Substring(0, 1));
    }
}")
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append("""", 0, 1).AppendLine();
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task AppendLine_AppendSubStringWithoutLength()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [|]new StringBuilder().AppendLine(""abc"".Substring(2));
    }
}")
                  .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append(""abc"", 2, ""abc"".Length - 2).AppendLine();
    }
}")
                  .ValidateAsync();
        }
    }
}

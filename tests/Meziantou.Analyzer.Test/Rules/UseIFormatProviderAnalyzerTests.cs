using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class UseIFormatProviderAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseIFormatProviderAnalyzer>();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Int32ToStringWithCultureInfo_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        1.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Int32ToStringWithoutCultureInfo_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]1.ToString();
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Use an overload of 'ToString' that has a 'System.IFormatProvider' parameter")
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task BooleanToStringWithoutCultureInfo_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        true.ToString();
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task SystemGuidToStringWithoutCultureInfo_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        default(System.Guid).ToString();
        default(System.Guid).ToString(""D"");
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Int32ParseWithoutCultureInfo_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]int.Parse("""");
        [||]int.Parse("""", System.Globalization.NumberStyles.Any);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Use an overload of 'Parse' that has a 'System.IFormatProvider' parameter")
                  .ShouldReportDiagnosticWithMessage("Use an overload of 'Parse' that has a 'System.IFormatProvider' parameter")
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task SingleTryParseWithoutCultureInfo_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]float.TryParse("""", out _);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Use an overload of 'TryParse' that has a 'System.IFormatProvider' parameter")
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task DateTimeTryParseWithoutCultureInfo_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]float.TryParse("""", out _);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Use an overload of 'TryParse' that has a 'System.IFormatProvider' parameter")
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task StringToLower_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]"""".ToLower();
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Use an overload of 'ToLower' that has a 'System.Globalization.CultureInfo' parameter")
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task StringBuilderAppendFormat_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]new System.Text.StringBuilder().AppendFormat(""{0}"", 10);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("Use an overload of 'AppendFormat' that has a 'System.IFormatProvider' parameter")
                  .ValidateAsync();
        }
    }
}

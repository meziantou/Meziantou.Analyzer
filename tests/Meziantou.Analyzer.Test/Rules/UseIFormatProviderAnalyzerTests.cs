using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseIFormatProviderAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseIFormatProviderAnalyzer>();
    }

    [Fact]
    public async Task Int32ToStringWithCultureInfo_ShouldNotReportDiagnostic()
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

    [Fact]
    public async Task Int32ToStringWithoutCultureInfo_ShouldReportDiagnostic()
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

    [Fact]
    public async Task BooleanToStringWithoutCultureInfo_ShouldNotReportDiagnostic()
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

    [Fact]
    public async Task SystemGuidToStringWithoutCultureInfo_ShouldNotReportDiagnostic()
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

    [Fact]
    public async Task SystemCharToStringWithoutCultureInfo_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        ' '.ToString();
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Int32ParseWithoutCultureInfo_ShouldReportDiagnostic()
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

    [Fact]
    public async Task SingleTryParseWithoutCultureInfo_ShouldReportDiagnostic()
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

    [Fact]
    public async Task DateTimeTryParseWithoutCultureInfo_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]System.DateTime.TryParse("""", out _);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload of 'TryParse' that has a 'System.IFormatProvider' parameter")
              .ValidateAsync();
    }

    [Fact]
    public async Task DateTimeOffsetTryParseWithoutCultureInfo_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]System.DateTimeOffset.TryParse("""", out _);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload of 'TryParse' that has a 'System.IFormatProvider' parameter")
              .ValidateAsync();
    }

    [Fact]
    public async Task StringToLower_ShouldReportDiagnostic()
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

    [Fact]
    public async Task StringBuilderAppendFormat_ShouldReportDiagnostic()
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

    [Fact]
    public async Task EnumValueToString()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        _ = A.Value1.ToString();
    }
}

enum A
{
   Value1
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task EnumToString()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test(System.Enum value)
    {
        _ = value.ToString();
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InvariantDateTimeFormat()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        _ = default(System.DateTime).ToString(""o"");
        _ = default(System.DateTime).ToString(""O"");
        _ = default(System.DateTime).ToString(""r"");
        _ = default(System.DateTime).ToString(""R"");
        _ = default(System.DateTime).ToString(""s"");
        _ = default(System.DateTime).ToString(""u"");
        _ = default(System.DateTimeOffset).ToString(""o"");
        _ = default(System.DateTimeOffset).ToString(""O"");
        _ = default(System.DateTimeOffset).ToString(""r"");
        _ = default(System.DateTimeOffset).ToString(""R"");
        _ = default(System.DateTimeOffset).ToString(""s"");
        _ = default(System.DateTimeOffset).ToString(""u"");
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
    [Fact]
    public async Task DateTimeToString()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        _ = [||]default(System.DateTime).ToString(""yyyy"");
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DateTimeTryParseInvariantFormat()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        System.DateTime.TryParse(""o"", out _);
        System.DateTime.TryParse(""O"", out _);
        System.DateTime.TryParse(""r"", out _);
        System.DateTime.TryParse(""R"", out _);
        System.DateTime.TryParse(""s"", out _);
        System.DateTime.TryParse(""u"", out _);
        System.DateTimeOffset.TryParse(""o"", out _);
        System.DateTimeOffset.TryParse(""O"", out _);
        System.DateTimeOffset.TryParse(""r"", out _);
        System.DateTimeOffset.TryParse(""R"", out _);
        System.DateTimeOffset.TryParse(""s"", out _);
        System.DateTimeOffset.TryParse(""u"", out _);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

}

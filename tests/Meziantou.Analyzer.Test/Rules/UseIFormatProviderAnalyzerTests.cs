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
        [||](-1).ToString();
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload of 'ToString' that has a 'System.IFormatProvider' parameter")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task Int32_PositiveToStringWithoutCultureInfo_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        1.ToString();
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
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
    public async Task SystemTimeSpanToStringWithoutCultureInfo_FormatC_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        System.TimeSpan.Zero.ToString(""C"");
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
    
    [Fact]
    public async Task SystemTimeSpanToStringWithoutCultureInfo_FormatG_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]System.TimeSpan.Zero.ToString(""G"");
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
    public async Task StringBuilder_AppendLine_AllStringParams()
    {
        const string SourceCode = """
class TypeName
{
    public void Test(System.Text.StringBuilder sb)
    {
        var str = "";
        sb.AppendLine($"foo{str}var{str}");
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringBuilder_AppendLine_AllStringParams_Net7()
    {
        const string SourceCode = """
using System;
class TypeName
{
    public void Test(System.Text.StringBuilder sb)
    {
        var str = "";
        sb.AppendLine($"foo{str}var{str}{'a'}{Guid.NewGuid()}");
    }
}
""";
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task StringBuilder_AppendLine_Int32Params_Net7()
    {
        const string SourceCode = """
class TypeName
{
    public void Test(System.Text.StringBuilder sb)
    {
        int value = 0;
        [||]sb.AppendLine($"foo{value}");
    }
}
""";
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
#endif

    [Theory]
    [InlineData("o")]
    [InlineData("O")]
    [InlineData("r")]
    [InlineData("R")]
    [InlineData("s")]
    [InlineData("u")]
    public async Task StringBuilder_AppendLine_DateTime_InvariantFormat_Net7(string format)
    {
        var sourceCode = $$"""
class TypeName
{
    public void Test(System.Text.StringBuilder sb)
    {
        System.DateTime value = default;
        sb.AppendLine($"foo{value:{{format}}}");
    }
}
""";
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task StringBuilder_AppendLine_DateTime_Net7()
    {
        var sourceCode = """
class TypeName
{
    public void Test(System.Text.StringBuilder sb)
    {
        System.DateTime value = default;
        [||]sb.AppendLine($"foo{value:yyyy}");
    }
}
""";
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
#endif

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
    public async Task GuidParse()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        System.Guid.Parse(""o"");
        System.Guid.TryParse(""o"", out _);
    }
}";
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NullableInt32ToStringWithCultureInfo()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        ((int?)1)?.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
    
    [Fact]
    public async Task NullableInt32ToStringWithoutCultureInfo()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        int? i = -1;
        [||]i.ToString();
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
    
    [Fact]
    public async Task NullableInt32ToStringWithoutCultureInfo_DisabledConfig()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        ((int?)1).ToString();
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0011.consider_nullable_types", "false")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task StringFormat_ArgsAreNonCultureSensitive()
    {
        var sourceCode = $$"""
class TypeName
{
    public void Test()
    {
        _ = string.Format("", "test", 1, 'c');
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
    
    [Fact]
    public async Task StringFormat_AlreadyHasFormatProvider()
    {
        var sourceCode = $$"""
class TypeName
{
    public void Test()
    {
        _ = string.Format(default(System.IFormatProvider), "", -1);
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
    
    [Fact]
    public async Task StringFormat_NoArgument()
    {
        var sourceCode = $$"""
class TypeName
{
    public void Test()
    {
        _ = string.Format("");
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
    
    [Fact]
    public async Task StringFormat_Report()
    {
        var sourceCode = $$"""
class TypeName
{
    public void Test()
    {
        _ = [||]string.Format("", -1);
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
    
    [Fact]
    public async Task StringFormat_ManyArgs_Report()
    {
        var sourceCode = $$"""
class TypeName
{
    public void Test()
    {
        _ = [||]string.Format("", 0, 0, 0, 0, 0, 0, -1, 0 ,0 ,0, 0);
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
}

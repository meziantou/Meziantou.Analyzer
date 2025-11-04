using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseIFormatProviderAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseIFormatProviderAnalyzer>()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithTargetFramework(TargetFramework.NetLatest)
            .AddMeziantouAttributes();
    }

    [Fact]
    public async Task Int32ToStringWithCultureInfo_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("1.ToString(System.Globalization.CultureInfo.InvariantCulture);")
              .ValidateAsync();
    }

    [Fact]
    public async Task Int32ToStringWithoutCultureInfo_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("[||](-1).ToString();")
              .ShouldReportDiagnosticWithMessage("Use an overload of 'ToString' that has a 'System.IFormatProvider' parameter")
              .ValidateAsync();
    }

    [Fact]
    public async Task Int32_PositiveToStringWithoutCultureInfo_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("1.ToString();")
              .ValidateAsync();
    }

    [Theory]
    [InlineData(""" (-1).ToString("x") """)]
    [InlineData(""" (-1).ToString("x8") """)]
    [InlineData(""" (-1).ToString("X" )""")]
    [InlineData(""" (-1).ToString("X8") """)]
    [InlineData(""" (-1).ToString("B") """)]
    [InlineData(""" true.ToString() """)]
    [InlineData(""" default(System.Guid).ToString() """)]
    [InlineData(""" default(System.Guid).ToString("D") """)]
    [InlineData(""" System.TimeSpan.Zero.ToString() """)]
    [InlineData(""" System.TimeSpan.Zero.ToString("c") """)]
    [InlineData(""" System.TimeSpan.Zero.ToString("T") """)]
    [InlineData(""" [||]System.TimeSpan.Zero.ToString("G") """)]
    [InlineData(""" ' '.ToString(); """)]
    [InlineData(""" [||]System.DateTime.TryParse("", out _) """)]
    [InlineData(""" [||]System.DateTimeOffset.TryParse("", out _) """)]
    [InlineData(""" [||]"".ToLower() """)]
    [InlineData(""" [||]new System.Text.StringBuilder().AppendFormat("{0}", 10) """)]
    [InlineData(""" System.DayOfWeek.Monday.ToString() """)]
    [InlineData(""" default(System.DateTime).ToString("o") """)]
    [InlineData(""" default(System.DateTime).ToString("O") """)]
    [InlineData(""" default(System.DateTime).ToString("r") """)]
    [InlineData(""" default(System.DateTime).ToString("R") """)]
    [InlineData(""" default(System.DateTime).ToString("s") """)]
    [InlineData(""" default(System.DateTime).ToString("u") """)]
    [InlineData(""" default(System.DateTimeOffset).ToString("o") """)]
    [InlineData(""" default(System.DateTimeOffset).ToString("O") """)]
    [InlineData(""" default(System.DateTimeOffset).ToString("r") """)]
    [InlineData(""" default(System.DateTimeOffset).ToString("R") """)]
    [InlineData(""" default(System.DateTimeOffset).ToString("s") """)]
    [InlineData(""" default(System.DateTimeOffset).ToString("u") """)]
    [InlineData(""" [||]default(System.DateTime).ToString("yyyy") """)]
    [InlineData(""" System.Guid.Parse("o") """)]
    [InlineData(""" System.Guid.TryParse("o", out _) """)]
    [InlineData(""" ((int?)1)?.ToString(System.Globalization.CultureInfo.InvariantCulture) """)]
    [InlineData(""" string.Format("", "test", 1, 'c') """)]
    [InlineData(""" string.Format(default(System.IFormatProvider), "", -1) """)]
    [InlineData(""" string.Format("") """)]
    [InlineData(""" [||]string.Format("", -1) """)]
    [InlineData(""" [||]string.Format("", 0, 0, 0, 0, 0, 0, -1, 0 ,0 ,0, 0) """)]
    [InlineData(""" System.Convert.ToChar((object)null) """)]
    [InlineData(""" System.Convert.ToChar("") """)]
    [InlineData(""" System.Convert.ToBoolean((object)null) """)]
    [InlineData(""" System.Convert.ToBoolean("") """)]
    public async Task Tests(string expression)
    {
        await CreateProjectBuilder()
              .WithSourceCode(expression + ";")
              .ValidateAsync();
    }

    [Fact]
    public async Task SystemTimeSpanImplicitToStringWithoutCultureInfo_InterpolatedString_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            var timeSpan = System.TimeSpan.FromSeconds(1);
            var myString = $"This is a test: {timeSpan}";
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Int32ParseWithoutCultureInfo_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            [||]int.Parse("");
            [||]int.Parse("", System.Globalization.NumberStyles.Any);
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload of 'Parse' that has a 'System.IFormatProvider' parameter")
              .ShouldReportDiagnosticWithMessage("Use an overload of 'Parse' that has a 'System.IFormatProvider' parameter")
              .ValidateAsync();
    }

    [Fact]
    public async Task SingleTryParseWithoutCultureInfo_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            [||]float.TryParse("", out _);
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload of 'TryParse' that has a 'System.IFormatProvider' parameter")
              .ValidateAsync();
    }

    [Fact]
    public async Task EnumToString()
    {
        const string SourceCode = """
            System.Enum value = default;
            _ = value.ToString();
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringBuilder_AppendLine_AllStringParams()
    {
        const string SourceCode = """
            var sb = new System.Text.StringBuilder();
            var str = "";
            sb.AppendLine($"foo{str}var{str}");
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
            var sb = new System.Text.StringBuilder();
            var str = "";
            sb.AppendLine($"foo{str}var{str}{'a'}{Guid.NewGuid()}");
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
            var sb = new System.Text.StringBuilder();
            int value = 0;
            [||]sb.AppendLine($"foo{value}");
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
            var sb = new System.Text.StringBuilder();
            System.DateTime value = default;
            sb.AppendLine($"foo{value:{{format}}}");
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
            var sb = new System.Text.StringBuilder();
            System.DateTime value = default;
            [||]sb.AppendLine($"foo{value:yyyy}");
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task NullableInt32ToStringWithoutCultureInfo()
    {
        const string SourceCode = """
            int? i = -1;
            [||]i.ToString();
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NullableInt32ToStringWithoutCultureInfo_DisabledConfig()
    {
        const string SourceCode = """
            ((int?)1).ToString();
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0011.consider_nullable_types", "false")
              .ValidateAsync();
    }

    [Fact]
    public async Task CultureInsensitiveTypeAttribute_Assembly()
    {
        var sourceCode = """
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime))]
_ = new System.DateTime().ToString();
_ = new System.DateTime().ToString("whatever");
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CultureInsensitiveTypeAttribute_Assembly_Format()
    {
        var sourceCode = """
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), "custom")]
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), "")]
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), null)]
_ = new System.DateTime().ToString("custom");
_ = new System.DateTime().ToString("");
_ = [|new System.DateTime().ToString("dummy")|];
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CultureInsensitiveTypeAttribute_Assembly_Format_null1()
    {
        var sourceCode = """
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), null)]
_ = [|new System.DateTime().ToString("dummy")|];
_ = new System.DateTime().ToString(format: null);
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ToString_IFormattable()
    {
        var sourceCode = """
_ = [|new Sample().ToString()|];

class Sample : System.IFormattable
{
    public override string ToString() => throw null;
    public string ToString(string format, System.IFormatProvider formatProvider) => throw null;
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ToString_WithIFormatProviderOverload_WithoutIFormattable()
    {
        var sourceCode = """
_ = [|new Location().ToString()|];

class Location
{
    public override string ToString() => throw null;
    public string ToString(System.IFormatProvider formatProvider) => throw null;
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringHandler_CultureSensitiveFormat_ShouldReport()
    {
        var sourceCode = """
using System;
using System.Runtime.CompilerServices;

[||]A.Print($"{DateTime.Now:D}");

class A
{
    public static void Print(ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
    public static void Print(IFormatProvider provider, ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringHandler_CultureInvariantFormat_ShouldNotReport()
    {
        var sourceCode = """
using System;
using System.Runtime.CompilerServices;

A.Print($"{DateTime.Now:o}");

class A
{
    public static void Print(ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
    public static void Print(IFormatProvider provider, ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringHandler_NoFormattableTypes_ShouldNotReport()
    {
        var sourceCode = """
using System;
using System.Runtime.CompilerServices;

A.Print($"XXX");

class A
{
    public static void Print(ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
    public static void Print(IFormatProvider provider, ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringHandler_MixedFormats_ShouldReport()
    {
        var sourceCode = """
using System;
using System.Runtime.CompilerServices;

[||]A.Print($"{DateTime.Now:o} | {DateTime.Now:D}");

class A
{
    public static void Print(ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
    public static void Print(IFormatProvider provider, ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringHandler_CustomTypeWithAttribute_CultureInvariantFormat_ShouldNotReport()
    {
        var sourceCode = """
using System;
using System.Runtime.CompilerServices;
using Meziantou.Analyzer.Annotations;

A.Print($"{new Bar():o}");

class A
{
    public static void Print(ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
    public static void Print(IFormatProvider provider, ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
}

[CultureInsensitiveType(format: "o")]
sealed class Bar : IFormattable
{
    public string ToString(string? format, IFormatProvider? formatProvider) => string.Empty;
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringHandler_CustomTypeWithAttribute_CultureSensitiveFormat_ShouldReport()
    {
        var sourceCode = """
using System;
using System.Runtime.CompilerServices;
using Meziantou.Analyzer.Annotations;

[||]A.Print($"{new Bar():D}");

class A
{
    public static void Print(ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
    public static void Print(IFormatProvider provider, ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
}

[CultureInsensitiveType(format: "o")]
sealed class Bar : IFormattable
{
    public string ToString(string? format, IFormatProvider? formatProvider) => string.Empty;
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FormattableString_CultureSensitiveFormat_ShouldReport()
    {
        var sourceCode = """
using System;

[||]A.Sample($"{DateTime.Now:D}");

class A
{
    public static void Sample(FormattableString value) => throw null;
    public static void Sample(IFormatProvider format, FormattableString value) => throw null;
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FormattableString_CultureInvariantFormat_ShouldNotReport()
    {
        var sourceCode = """
using System;

A.Sample($"{DateTime.Now:o}");

class A
{
    public static void Sample(FormattableString value) => throw null;
    public static void Sample(IFormatProvider format, FormattableString value) => throw null;
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringHandler_NoOverload_ShouldNotReport()
    {
        var sourceCode = """
using System;
using System.Runtime.CompilerServices;

A.Print($"{DateTime.Now:D}");

class A
{
    public static void Print(ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
}

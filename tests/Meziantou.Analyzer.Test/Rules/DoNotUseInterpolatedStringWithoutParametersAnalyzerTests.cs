using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseInterpolatedStringWithoutParametersAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotUseInterpolatedStringWithoutParametersAnalyzer>()
            .WithCodeFixProvider<DoNotUseInterpolatedStringWithoutParametersFixer>()
            .WithTargetFramework(TargetFramework.Net6_0);
    }

    [Fact]
    public async Task InterpolatedStringWithoutParameters_StringCreate_ShouldReportDiagnostic()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = string.Create(CultureInfo.InvariantCulture, [|$"string without parameters."|]);
    }
}
""";

        const string Fix = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = "string without parameters.";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringWithParameters_StringCreate_NoDiagnostic()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = string.Create(CultureInfo.InvariantCulture, $"Current time is {DateTime.Now:D}.");
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringWithParameters_StringBuilder_NoDiagnostic()
    {
        const string SourceCode = """
using System.Text;

class TypeName
{
    public void Test()
    {
        var sb = new StringBuilder();
        var value = 42;
        sb.Append($"Value: {value}");
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task VerbatimInterpolatedStringWithoutParameters_ShouldReportDiagnostic()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = string.Create(CultureInfo.InvariantCulture, [|$@"C:\path\to\file"|]);
    }
}
""";

        const string Fix = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = @"C:\path\to\file";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringWithoutParameters_FormattableString_NoDiagnostic()
    {
        const string SourceCode = """
using System;

class TypeName
{
    public void Test()
    {
        FormattableString fs = $"literal";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Net5_NoDiagnostic()
    {
        const string SourceCode = """
using System;

class TypeName
{
    public void Test()
    {
        FormattableString fs = $"string without parameters.";
    }
}
""";
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net5_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

#if CSHARP11_OR_GREATER
    [Fact]
    public async Task RawStringLiteral_ShouldReportDiagnostic()
    {
        const string SourceCode = """"
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = string.Create(CultureInfo.InvariantCulture, [|$"""literal"""|]);
    }
}
"""";

        const string Fix = """"
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = """literal""";
    }
}
"""";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }
#endif
}

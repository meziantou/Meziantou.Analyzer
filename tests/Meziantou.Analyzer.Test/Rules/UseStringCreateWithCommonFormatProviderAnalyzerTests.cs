using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringCreateWithCommonFormatProviderAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseStringCreateWithCommonFormatProviderAnalyzer>();
    }

    [Fact]
    public async Task MultipleToStringWithSameInvariantCulture_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = [|$"{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}"|];
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IssueExample_SameCulture_ShouldReport()
    {
        // This is the exact example from the issue that should be flagged
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = [|$"{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}"|];
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IssueExample_DifferentCultures_NoDiagnostic()
    {
        // This is the example from the issue that should NOT be flagged (different cultures)
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var y = $"{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}{DateTime.Now.ToString("D", CultureInfo.CurrentCulture)}";
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultipleToStringWithDifferentCultures_NoDiagnostic()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = $"{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}{DateTime.Now.ToString("D", CultureInfo.CurrentCulture)}";
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SingleToStringWithFormatProvider_NoDiagnostic()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = $"{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}";
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultipleToStringWithSameCurrentCulture_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = [|$"{DateTime.Now.ToString("D", CultureInfo.CurrentCulture)}{DateTime.Now.ToString("D", CultureInfo.CurrentCulture)}"|];
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultipleToStringWithSameParameter_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test(CultureInfo culture)
    {
        var x = [|$"{DateTime.Now.ToString("D", culture)}{DateTime.Now.ToString("D", culture)}"|];
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultipleToStringWithSameLocalVariable_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var culture = CultureInfo.InvariantCulture;
        var x = [|$"{DateTime.Now.ToString("D", culture)}{DateTime.Now.ToString("D", culture)}"|];
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ThreeToStringWithSameFormatProvider_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = [|$"{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}{DateTime.Now.ToString("T", CultureInfo.InvariantCulture)}{DateTime.Now.ToString("d", CultureInfo.InvariantCulture)}"|];
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MixedToStringWithAndWithoutFormatProvider_NoDiagnostic()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = $"{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}{42}";
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringWithLiterals_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = [|$"Date: {DateTime.Now.ToString("D", CultureInfo.InvariantCulture)} Time: {DateTime.Now.ToString("T", CultureInfo.InvariantCulture)}"|];
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FormattableStringConversion_NoDiagnostic()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        FormattableString x = $"{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}";
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AlreadyUsedWithStringCreate_NoDiagnostic()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = string.Create(CultureInfo.InvariantCulture, $"{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}");
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DifferentTypesWithSameFormatProvider_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = [|$"{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}{42.5.ToString("F2", CultureInfo.InvariantCulture)}"|];
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ToStringWithoutFormatParameter_NoDiagnostic()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = $"{DateTime.Now.ToString()}{DateTime.Now.ToString()}";
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultipleToStringOneWithOnlyFormat_NoDiagnostic()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = $"{DateTime.Now.ToString("D", CultureInfo.InvariantCulture)}{DateTime.Now.ToString("D")}";
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}

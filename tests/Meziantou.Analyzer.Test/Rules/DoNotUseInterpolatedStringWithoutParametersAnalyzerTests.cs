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
            .WithCodeFixProvider<DoNotUseInterpolatedStringWithoutParametersFixer>();
    }

    [Fact]
    public async Task InterpolatedStringWithoutParameters_ShouldReportDiagnostic()
    {
        const string SourceCode = """
class TypeName
{
    public void Test()
    {
        var x = [|$"Required attribute 'output' not found."|];
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RegularString_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
class TypeName
{
    public void Test()
    {
        var x = "Required attribute 'output' not found.";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringWithParameters_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
class TypeName
{
    public void Test()
    {
        var name = "output";
        var x = $"Required attribute '{name}' not found.";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringWithoutParameters_AssignedToFormattableString_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
using System;

class TypeName
{
    public void Test()
    {
        FormattableString x = $"Required attribute 'output' not found.";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringWithoutParameters_ConvertedToFormattableString_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
using System;

class TypeName
{
    public void Test(FormattableString fs)
    {
    }

    public void Run()
    {
        Test($"Required attribute 'output' not found.");
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task InterpolatedStringWithoutParameters_CustomInterpolatedStringHandler_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
class TypeName
{
    public void Test(CustomInterpolatedStringHandler handler)
    {
    }

    public void Run()
    {
        Test($"Required attribute 'output' not found.");
    }
}

[System.Runtime.CompilerServices.InterpolatedStringHandler]
public struct CustomInterpolatedStringHandler
{
    public CustomInterpolatedStringHandler(int literalLength, int formattedCount)
    {
    }

    public void AppendLiteral(string s)
    {
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithTargetFramework(TargetFramework.Net6_0)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task InterpolatedStringWithoutParameters_InReturnStatement_ShouldReportDiagnostic()
    {
        const string SourceCode = """
class TypeName
{
    public string Test()
    {
        return [|$"Required attribute 'output' not found."|];
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringWithoutParameters_InMethodArgument_ShouldReportDiagnostic()
    {
        const string SourceCode = """
class TypeName
{
    public void Test(string message)
    {
    }

    public void Run()
    {
        Test([|$"Required attribute 'output' not found."|]);
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringWithEmptyInterpolation_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
class TypeName
{
    public void Test()
    {
        var name = "test";
        var x = $"Value: {name}";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_ShouldConvertToRegularString()
    {
        const string SourceCode = """
class TypeName
{
    public void Test()
    {
        var x = [|$"Required attribute 'output' not found."|];
    }
}
""";

        const string FixedCode = """
class TypeName
{
    public void Test()
    {
        var x = "Required attribute 'output' not found.";
    }
}
""";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(FixedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_ShouldHandleEscapedCharacters()
    {
        const string SourceCode = """
class TypeName
{
    public void Test()
    {
        var x = [|$"Line 1\nLine 2"|];
    }
}
""";

        const string FixedCode = """
class TypeName
{
    public void Test()
    {
        var x = "Line 1\nLine 2";
    }
}
""";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(FixedCode)
              .ValidateAsync();
    }

#if CSHARP11_OR_GREATER
    [Fact]
    public async Task RawInterpolatedStringWithoutParameters_ShouldReportDiagnostic()
    {
        const string SourceCode = """"
class TypeName
{
    public void Test()
    {
        _ = [|$"""
            Sample
            """|];
    }
}
"""";

        const string FixedCode = """"
class TypeName
{
    public void Test()
    {
        _ = """
            Sample
            """;
    }
}
"""";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11)
              .ShouldFixCodeWith(FixedCode)
              .ValidateAsync();
    }
#endif
}

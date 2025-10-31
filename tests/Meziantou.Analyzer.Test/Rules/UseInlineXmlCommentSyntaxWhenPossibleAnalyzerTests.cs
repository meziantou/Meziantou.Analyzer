using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseInlineXmlCommentSyntaxWhenPossibleAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseInlineXmlCommentSyntaxWhenPossibleAnalyzer>()
            .WithCodeFixProvider<UseInlineXmlCommentSyntaxWhenPossibleFixer>()
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    [Fact]
    public async Task SingleLineDescription_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
/// [|<summary>
/// description
/// </summary>|]
class Sample { }
""")
              .ShouldFixCodeWith("""
/// <summary>description</summary>
class Sample { }
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task MultiLineDescription_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
/// <summary>
/// description line 1
/// description line 2
/// </summary>
class Sample { }
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task AlreadyInline_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
/// <summary>description</summary>
class Sample { }
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task ParamSingleLine_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
class Sample
{
    /// [|<param name="value">
    /// The value
    /// </param>|]
    public void Method(int value) { }
}
""")
              .ShouldFixCodeWith("""
class Sample
{
    /// <param name="value">The value</param>
    public void Method(int value) { }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task RemarksSingleLine_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
/// [|<remarks>
/// This is a remark
/// </remarks>|]
class Sample { }
""")
              .ShouldFixCodeWith("""
/// <remarks>This is a remark</remarks>
class Sample { }
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task ReturnsSingleLine_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
class Sample
{
    /// [|<returns>
    /// The result
    /// </returns>|]
    public int Method() => 42;
}
""")
              .ShouldFixCodeWith("""
class Sample
{
    /// <returns>The result</returns>
    public int Method() => 42;
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task InnerXmlElements_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
/// <summary>
/// This has <c>
/// code
/// </c> inside
/// </summary>
class Sample { }
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task EmptyContent_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
/// [|<summary>
/// </summary>|]
class Sample { }
""")
              .ShouldFixCodeWith("""
/// <summary></summary>
class Sample { }
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task TypeParamSingleLine_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
/// [|<typeparam name="T">
/// The type parameter
/// </typeparam>|]
class Sample<T> { }
""")
              .ShouldFixCodeWith("""
/// <typeparam name="T">The type parameter</typeparam>
class Sample<T> { }
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task ExceptionSingleLine_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
class Sample
{
    /// [|<exception cref="System.ArgumentNullException">
    /// Thrown when argument is null
    /// </exception>|]
    public void Method(string value) { }
}
""")
              .ShouldFixCodeWith("""
class Sample
{
    /// <exception cref="System.ArgumentNullException">Thrown when argument is null</exception>
    public void Method(string value) { }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task ValueSingleLine_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
class Sample
{
    /// [|<value>
    /// The property value
    /// </value>|]
    public int Property { get; set; }
}
""")
              .ShouldFixCodeWith("""
class Sample
{
    /// <value>The property value</value>
    public int Property { get; set; }
}
""")
              .ValidateAsync();
    }
}

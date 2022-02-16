using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringComparisonAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseStringComparisonAnalyzer>("MA0074")
            .WithCodeFixProvider<UseStringComparisonFixer>();
    }

    [Fact]
    public async Task Equals_String_string_StringComparison_ShouldNotReportDiagnosticWhenStringComparisonIsSpecifiedAsync()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        var a = ""test"";
        string.Equals(a, ""v"", System.StringComparison.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IndexOf_String_StringComparison_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        ""a"".IndexOf(""v"", System.StringComparison.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IndexOf_String_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]""a"".IndexOf(""v"");
    }
}";
        const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        ""a"".IndexOf(""v"", System.StringComparison.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload of 'IndexOf' that has a StringComparison parameter")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StartsWith_String_StringComparison_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        ""a"".StartsWith(""v"", System.StringComparison.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StartsWith_String_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]""a"".StartsWith(""v"");
    }
}";
        const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        ""a"".StartsWith(""v"", System.StringComparison.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload of 'StartsWith' that has a StringComparison parameter")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Compare_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]string.Compare(""a"", ""v"");
    }
}";
        const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        string.Compare(""a"", ""v"", System.StringComparison.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use an overload of 'Compare' that has a StringComparison parameter")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Compare_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        string.Compare(""a"", ""v"", ignoreCase: true);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IndexOf_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        """".IndexOf("""", 0, System.StringComparison.Ordinal);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ExcludeWhenInAnExpressionContext()
    {
        const string SourceCode = @"
using System;
using System.Linq.Expressions;
class TypeName
{
    void WithSomething()
    {
        _ = (Expression<Func<Something, bool>>)(s => s.SomeField.Contains(""""));
    }

    public class Something
    {
        public string SomeField { get; set; }
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}

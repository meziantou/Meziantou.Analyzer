using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringEqualsAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseStringEqualsAnalyzer>()
            .WithCodeFixProvider<UseStringEqualsFixer>();
    }

    [Fact]
    public async Task Equals_StringLiteral_stringLiteral_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        var a = [||]""a"" == ""v"";
    }
}";
        const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        var a = string.Equals(""a"", ""v"", System.StringComparison.Ordinal);
    }
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldReportDiagnosticWithMessage("Use string.Equals instead of Equals operator")
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task NotEquals_StringLiteral_stringLiteral_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        var a = [||]""a"" != ""v"";
    }
}";
        const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        var a = !string.Equals(""a"", ""v"", System.StringComparison.Ordinal);
    }
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldReportDiagnosticWithMessage("Use string.Equals instead of NotEquals operator")
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_StringVariable_stringLiteral_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test(string str)
    {
        var a = [||]str == ""v"";
    }
}";
        const string CodeFix = @"
class TypeName
{
    public void Test(string str)
    {
        var a = string.Equals(str, ""v"", System.StringComparison.Ordinal);
    }
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldReportDiagnosticWithMessage("Use string.Equals instead of Equals operator")
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_ObjectVariable_stringLiteral_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        object str = """";
        var a = str == ""v"";
    }
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_stringLiteral_null_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        var a = ""a"" == null;
        var b = null == ""a"";
    }
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_InIQueryableMethod_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        IQueryable<string> query = null;
        query = query.Where(i => i == ""test"");
    }
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_EmptyString_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        var a = """" == ""v"";
    }
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_StringEmpty_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        var a = string.Empty == ""v"";
    }
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task Replace_Meziantou_Framework_EqualsOrdinal()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        var a = [|""a"" == ""b""|];
    }
}";
        const string CodeFix = @"
using Meziantou.Framework;

class TypeName
{
    public void Test()
    {
        var a = ""a"".EqualsOrdinal(""b"");
    }
}";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(index: 2, CodeFix)
            .AddNuGetReference("Meziantou.Framework", "3.0.23", "lib/net6.0/")
            .ValidateAsync();
    }

    [Fact]
    public async Task Replace_Meziantou_Framework_EqualsIgnoreCase()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        var a = [|""a"" == ""b""|];
    }
}";
        const string CodeFix = @"
using Meziantou.Framework;

class TypeName
{
    public void Test()
    {
        var a = ""a"".EqualsIgnoreCase(""b"");
    }
}";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(index: 3, CodeFix)
            .AddNuGetReference("Meziantou.Framework", "3.0.23", "lib/net6.0/")
            .ValidateAsync();
    }
}

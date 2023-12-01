using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class CommaAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<CommaAnalyzer>()
            .WithCodeFixProvider<CommaFixer>();
    }

    [Fact]
    public async Task OneLineDeclarationWithMissingTrailingComma_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public async System.Threading.Tasks.Task Test()
    {
        new TypeName() { A = 1 };
    }
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task MultipleLinesDeclarationWithTrailingComma_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public async System.Threading.Tasks.Task Test()
    {
        new TypeName()
        {
            A = 1,
            B = 2,
        };
    }
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task MultipleLinesDeclarationWithMissingTrailingComma_ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public async System.Threading.Tasks.Task Test()
    {
        new TypeName()
        {
            A = 1,
            [||]B = 2
        };
    }
}";
        const string CodeFix = @"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public async System.Threading.Tasks.Task Test()
    {
        new TypeName()
        {
            A = 1,
            B = 2,
        };
    }
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task EnumsWithLeadingComma()
    {
        const string SourceCode = @"
enum TypeName
{
    A = 1,
    B = 2,
}";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task EnumsWithoutLeadingComma()
    {
        const string SourceCode = @"
enum TypeName
{
    A = 1,
    [||]B = 2
}";
        const string CodeFix = @"
enum TypeName
{
    A = 1,
    B = 2,
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task AnonymousObjectWithLeadingComma()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        _ = new
        {
            A = 1,
            B = 2,
        };
    }
}";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task AnonymousObjectWithoutLeadingComma()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        _ = new
        {
            A = 1,
            [||]B = 2
        };
    }
}";
        const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        _ = new
        {
            A = 1,
            B = 2,
        };
    }
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task ImplicitCtorWithoutLeadingComma()
    {
        const string SourceCode = @"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public void Test()
    {
        TypeName a = new()
        {
            A = 1,
            [||]B = 2
        };
    }
}";
        const string CodeFix = @"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public void Test()
    {
        TypeName a = new()
        {
            A = 1,
            B = 2,
        };
    }
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

#if CSHARP12_OR_GREATER
    [Fact]
    public async Task CollectionExpressionWithoutLeadingComma()
    {
        const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        int[] a =
        [
            1,
            [||]2
        ];
    }
}";
        const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        int[] a =
        [
            1,
            2,
        ];
    }
}";
        await CreateProjectBuilder()
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }
#endif
}

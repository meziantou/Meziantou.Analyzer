using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStructLayoutAttributeAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseStructLayoutAttributeAnalyzer>()
            .WithCodeFixProvider<UseStructLayoutAttributeFixer>();
    }

    [Fact]
    public async Task SingleField_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"struct TypeName
{
    static int s_a;
    const int constant = 0;
    int a;
}";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task MissingAttribute_ShouldReportDiagnostic()
    {
        const string SourceCode = @"struct [||]TypeName
{
    int a;
    int b;
}";
        const string CodeFix = @"using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Auto)]
struct TypeName
{
    int a;
    int b;
}";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task AddAttributeShouldUseShortname()
    {
        const string SourceCode = @"using System.Runtime.InteropServices;
struct [||]TypeName
{
    int a;
    int b;
}";
        const string CodeFix = @"using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Auto)]
struct TypeName
{
    int a;
    int b;
}";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task WithAttribute_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Sequential)]
struct TypeName
{
    int a;
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task Enum_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
enum TypeName
{
    None,
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task WithReferenceType_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
struct TypeName
{
    string a;
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task Empty_ShouldNotReportDiagnostic()
    {
        const string SourceCode = @"
struct TypeName
{
}";
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task RecordStruct()
    {
        const string SourceCode = @"record struct [||]TypeName(int A, int B);";

        const string CodeFix = @"using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Auto)]
record struct TypeName(int A, int B);";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }
#endif
}

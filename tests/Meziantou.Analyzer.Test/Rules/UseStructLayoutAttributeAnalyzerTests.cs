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
        const string SourceCode = @"struct [|TypeName|]
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
struct [|TypeName|]
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
        const string SourceCode = """
            enum TypeName
            {
                None,
            }
            """;
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task WithReferenceType_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            struct TypeName
            {
                string a;
            }
            """;
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task Empty_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            struct TypeName
            {
            }
            """;
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task WithBoolFields_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            struct TypeName
            {
                bool a;
                bool b;
            }
            """;
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task WithCharFields_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            struct TypeName
            {
                char a;
                char b;
            }
            """;
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task WithDecimalFields_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            struct TypeName
            {
                decimal a;
                decimal b;
            }
            """;
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task WithIntPtrFields_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            struct [|TypeName|]
            {
                nint a;
                nint b;
            }
            """;
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task WithUIntPtrFields_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            struct [|TypeName|]
            {
                nuint a;
                nuint b;
            }
            """;
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task WithFloatAndDoubleFields_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            struct [|TypeName|]
            {
                float a;
                double b;
            }
            """;
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task WithEnumFields_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            enum MyEnum { A, B }
            struct [|TypeName|]
            {
                MyEnum a;
                MyEnum b;
            }
            """;
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task WithBlittableNestedStruct_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            struct Inner
            {
                int x;
            }
            struct [|TypeName|]
            {
                Inner a;
                int b;
            }
            """;
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task WithNonBlittableNestedStruct_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            struct Inner
            {
                bool x;
            }
            struct TypeName
            {
                Inner a;
                int b;
            }
            """;
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task WithMixedBlittableAndNonBlittable_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            struct TypeName
            {
                int a;
                bool b;
            }
            """;
        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task RecordStruct()
    {
        const string SourceCode = """
            record struct [|TypeName|](int A, int B);
            """;

#if ROSLYN_4_14_OR_GREATER
        const string CodeFix = """
            using System.Runtime.InteropServices;

            [StructLayout(LayoutKind.Auto)]
            record struct TypeName(int A, int B);
            """;
#else
        // Roslyn 4.8 adds the using directive with '\r\n' line endings, whatever the line endings of the document
        const string CodeFix = "using System.Runtime.InteropServices;\r\n\r\n[StructLayout(LayoutKind.Auto)]\nrecord struct TypeName(int A, int B);";
#endif

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }
}

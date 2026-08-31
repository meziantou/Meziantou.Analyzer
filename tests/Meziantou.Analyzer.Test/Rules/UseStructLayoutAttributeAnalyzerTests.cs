using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseStructLayoutAttributeAnalyzer,
    Meziantou.Analyzer.Rules.UseStructLayoutAttributeFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStructLayoutAttributeAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task SingleField_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct TypeName
            {
                static int s_a;
                const int constant = 0;
                int a;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingAttribute_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct {|MA0008:TypeName|}
            {
                int a;
                int b;
            }
            """;
        test.FixedCode = """
            using System.Runtime.InteropServices;

            [StructLayout(LayoutKind.Auto)]
            struct TypeName
            {
                int a;
                int b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddAttributeShouldUseShortname()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.InteropServices;
            struct {|MA0008:TypeName|}
            {
                int a;
                int b;
            }
            """;
        test.FixedCode = """
            using System.Runtime.InteropServices;

            [StructLayout(LayoutKind.Auto)]
            struct TypeName
            {
                int a;
                int b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithAttribute_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.InteropServices;
            [StructLayout(LayoutKind.Sequential)]
            struct TypeName
            {
                int a;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Enum_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            enum TypeName
            {
                None,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithReferenceType_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct TypeName
            {
                string a;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Empty_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct TypeName
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithBoolFields_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct TypeName
            {
                bool a;
                bool b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithCharFields_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct TypeName
            {
                char a;
                char b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithDecimalFields_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct TypeName
            {
                decimal a;
                decimal b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithIntPtrFields_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct {|MA0008:TypeName|}
            {
                nint a;
                nint b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithUIntPtrFields_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct {|MA0008:TypeName|}
            {
                nuint a;
                nuint b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithFloatAndDoubleFields_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct {|MA0008:TypeName|}
            {
                float a;
                double b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithEnumFields_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            enum MyEnum { A, B }
            struct {|MA0008:TypeName|}
            {
                MyEnum a;
                MyEnum b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithBlittableNestedStruct_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Inner
            {
                int x;
            }
            struct {|MA0008:TypeName|}
            {
                Inner a;
                int b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithNonBlittableNestedStruct_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task WithMixedBlittableAndNonBlittable_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            struct TypeName
            {
                int a;
                bool b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RecordStruct()
    {
        var test = CreateTest();
        test.TestCode = """
            record struct {|MA0008:TypeName|}(int A, int B);
            """;
        // Roslyn 4.8 inserts CRLF where the newer versions insert LF, and the testing library
        // compares the text of the fixed code exactly
#if ROSLYN_4_14_OR_GREATER
        test.FixedCode = """
            using System.Runtime.InteropServices;

            [StructLayout(LayoutKind.Auto)]
            record struct TypeName(int A, int B);
            """;
#endif

        return test.RunAsync();
    }
}

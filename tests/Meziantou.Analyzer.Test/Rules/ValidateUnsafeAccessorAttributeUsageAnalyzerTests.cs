using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.ValidateUnsafeAccessorAttributeUsageAnalyzer,
    Meziantou.Analyzer.Rules.ValidateUnsafeAccessorAttributeUsageFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ValidateUnsafeAccessorAttributeUsageAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task NotExternStaticMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                [System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.StaticMethod)]
                void {|MA0145:A|}() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalFunction_WithoutNameParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void A()
                {
                    // Local function name are mangle by the compiler, so the Name property is required
                    [UnsafeAccessor(UnsafeAccessorKind.Field)]
                    extern static ref int {|MA0146:B|}(System.Version a);
                }
            }
            """;
        test.FixedCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void A()
                {
                    // Local function name are mangle by the compiler, so the Name property is required
                    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "B")]
                    extern static ref int B(System.Version a);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalFunction_WithNameProperty()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void A()
                {
                    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_Major")]
                    extern static ref int B(System.Version a);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_TooManyParameters()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void A()
                {
                    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_Major")]
                    extern static ref int {|MA0145:B|}(System.Version a, int b);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_ReturnVoid()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void A()
                {
                    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_Major")]
                    extern static void {|MA0145:B|}(System.Version a);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_DoesNotReturnByRef()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void A()
                {
                    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_Major")]
                    extern static int {|MA0145:B|}(System.Version a);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_FirstParameterNotByRefForStruct()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void A()
                {
                    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_Major")]
                    extern static ref int {|MA0145:B|}(System.Int32 a);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_FirstParameterByRefForStruct()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            class Sample
            {
                void A()
                {
                    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_Major")]
                    extern static ref int B(ref System.Int32 a);
                }
            }
            """;

        return test.RunAsync();
    }
}

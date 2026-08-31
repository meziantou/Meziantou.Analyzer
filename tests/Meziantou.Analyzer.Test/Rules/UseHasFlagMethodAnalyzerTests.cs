using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseHasFlagMethodAnalyzer,
    Meziantou.Analyzer.Rules.UseHasFlagMethodFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseHasFlagMethodAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task EqualityCheck_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) == MyEnum.Flag1|};
            }
            """;
        test.FixedCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => value.HasFlag(MyEnum.Flag1);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EqualityCheck_ReversedAndOperands_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0192:(MyEnum.Flag1 & value) == MyEnum.Flag1|};
            }
            """;
        test.FixedCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value) => value.HasFlag(MyEnum.Flag1);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsPatternCheck_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) is MyEnum.Flag1|};
            }
            """;
        // A qualified enum member in a pattern is parsed as a type, whereas the fixer reuses the constant
        // the compiler binds it to, so the shape of the tree cannot be compared with the parsed one
        test.CodeActionValidationMode = CodeActionValidationMode.None;
        test.FixedCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => value.HasFlag(MyEnum.Flag1);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotEqualsCheck_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) != MyEnum.Flag1|};
            }
            """;
        test.FixedCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => !value.HasFlag(MyEnum.Flag1);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsNotPatternCheck_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) is not MyEnum.Flag1|};
            }
            """;
        test.FixedCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => !value.HasFlag(MyEnum.Flag1);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EqualsZeroCheck_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) == 0|};
            }
            """;
        test.FixedCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => !value.HasFlag(MyEnum.Flag1);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotEqualsZeroCheck_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) != 0|};
            }
            """;
        test.FixedCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => value.HasFlag(MyEnum.Flag1);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsPatternZeroCheck_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) is 0|};
            }
            """;
        test.FixedCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => !value.HasFlag(MyEnum.Flag1);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsNotPatternZeroCheck_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0192:(value & MyEnum.Flag1) is not 0|};
            }
            """;
        test.FixedCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => value.HasFlag(MyEnum.Flag1);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ZeroFlagEqualityCheck_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0201:(value & MyEnum.None) == MyEnum.None|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ZeroLiteralEqualityCheck_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0201:(value & MyEnum.None) == 0|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ZeroFlagNotEqualsCheck_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0201:(value & MyEnum.None) != MyEnum.None|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ZeroFlagIsPatternCheck_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0201:(value & MyEnum.None) is MyEnum.None|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ZeroFlagIsNotPatternCheck_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0201:(value & MyEnum.None) is not MyEnum.None|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasFlagZeroFlag_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0201:value.HasFlag(MyEnum.None)|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasFlagExplicitZeroCast_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value) => {|MA0201:value.HasFlag((MyEnum)0)|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DifferentFlag_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => (value & MyEnum.Flag1) == MyEnum.Flag2;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasFlagsExtensionZeroFlag_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            static class MyEnumExtensions
            {
                public static bool HasFlags(this MyEnum value, MyEnum flags) => {|MA0192:(value & flags) == flags|};
            }

            class Sample
            {
                bool M(MyEnum value) => value.HasFlags(MyEnum.None);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonZeroHasFlag_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value) => value.HasFlag(MyEnum.Flag1);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CombinedFlag_NotEqualsZero_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
                Flag1AndFlag2 = Flag1 | Flag2,
            }

            class Sample
            {
                bool M(MyEnum value) => (value & MyEnum.Flag1AndFlag2) != 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DifferentFlag_IsNotPattern_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value) => (value & MyEnum.Flag1) is not MyEnum.Flag2;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IntegerBitwiseCheck_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                bool M(int value) => (value & 1) == 1;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NullableEnum_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum? value) => (value & MyEnum.Flag1) == MyEnum.Flag1;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParameterFlag_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value, MyEnum comparand) => {|MA0192:(value & comparand) == comparand|};
            }
            """;
        test.FixedCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
                Flag2 = 2,
            }

            class Sample
            {
                bool M(MyEnum value, MyEnum comparand) => value.HasFlag(comparand);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParameterFlag_ReversedAndOperands_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value, MyEnum comparand) => {|MA0192:(comparand & value) == comparand|};
            }
            """;
        test.FixedCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value, MyEnum comparand) => value.HasFlag(comparand);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParameterFlag_NotEquals_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value, MyEnum comparand) => {|MA0192:(value & comparand) != comparand|};
            }
            """;
        test.FixedCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value, MyEnum comparand) => !value.HasFlag(comparand);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalFlag_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value)
                {
                    var comparand = MyEnum.Flag1;
                    return {|MA0192:(value & comparand) == comparand|};
                }
            }
            """;
        test.FixedCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value)
                {
                    var comparand = MyEnum.Flag1;
                    return value.HasFlag(comparand);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FieldFlag_ReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                private MyEnum _comparand;

                bool M(MyEnum value) => {|MA0192:(value & _comparand) == this._comparand|};
            }
            """;
        test.FixedCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                private MyEnum _comparand;

                bool M(MyEnum value) => value.HasFlag(this._comparand);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DifferentParameters_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                bool M(MyEnum value, MyEnum comparand, MyEnum other) => (value & comparand) == other;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DifferentInstanceFields_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                private MyEnum _comparand;

                bool M(MyEnum value, Sample other) => (value & _comparand) == other._comparand;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VolatileFieldFlag_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                private volatile MyEnum _comparand;

                bool M(MyEnum value) => (value & _comparand) == _comparand;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyFlag_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                private MyEnum Comparand => MyEnum.Flag1;

                bool M(MyEnum value) => (value & Comparand) == Comparand;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MethodCallFlag_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Flags]
            enum MyEnum
            {
                None = 0,
                Flag1 = 1,
            }

            class Sample
            {
                private MyEnum GetComparand() => MyEnum.Flag1;

                bool M(MyEnum value) => (value & GetComparand()) == GetComparand();
            }
            """;

        return test.RunAsync();
    }
}

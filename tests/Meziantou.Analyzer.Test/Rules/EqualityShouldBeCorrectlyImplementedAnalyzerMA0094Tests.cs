using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.EqualityShouldBeCorrectlyImplementedAnalyzer,
    Meziantou.Analyzer.Rules.EqualityShouldBeCorrectlyImplementedFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EqualityShouldBeCorrectlyImplementedAnalyzerMA0094Tests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task ClassImplementsNoInterfaceAndProvidesCompatibleCompareToMethod_DiagnosticIsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class {|MA0094:Test|} : IComparable<string>
            {
                public int CompareTo(string other) => throw null;
                public int CompareTo(Test other) => throw null;
                public static bool operator <(Test a, Test b) => throw null;
                public static bool operator <=(Test a, Test b) => throw null;
                public static bool operator >(Test a, Test b) => throw null;
                public static bool operator >=(Test a, Test b) => throw null;
                public static bool operator ==(Test a, Test b) => throw null;
                public static bool operator !=(Test a, Test b) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AlreadyImplemented()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Test : IComparable<Test>, IEquatable<Test>
            {
                public int CompareTo(string other) => throw null;
                public int CompareTo(Test other) => throw null;
                public bool Equals(Test other) => throw null;
                public override bool Equals(object other) => throw null;
                public static bool operator <(Test a, Test b) => throw null;
                public static bool operator <=(Test a, Test b) => throw null;
                public static bool operator >(Test a, Test b) => throw null;
                public static bool operator >=(Test a, Test b) => throw null;
                public static bool operator ==(Test a, Test b) => throw null;
                public static bool operator !=(Test a, Test b) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingIEquatable()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class {|MA0096:Test|} : IComparable<Test>
            {
                public int CompareTo(string other) => throw null;
                public int CompareTo(Test other) => throw null;
                public static bool operator <(Test a, Test b) => throw null;
                public static bool operator <=(Test a, Test b) => throw null;
                public static bool operator >(Test a, Test b) => throw null;
                public static bool operator >=(Test a, Test b) => throw null;
                public static bool operator ==(Test a, Test b) => throw null;
                public static bool operator !=(Test a, Test b) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MA0094_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class {|MA0094:Test|} : IComparable<string>
            {
                public int CompareTo(string other) => throw null;
                public int CompareTo(Test other) => throw null;
                public static bool operator <(Test a, Test b) => throw null;
                public static bool operator <=(Test a, Test b) => throw null;
                public static bool operator >(Test a, Test b) => throw null;
                public static bool operator >=(Test a, Test b) => throw null;
                public static bool operator ==(Test a, Test b) => throw null;
                public static bool operator !=(Test a, Test b) => throw null;
            }
            """;
        // Fixing MA0094 makes MA0096 report on the result, which the iterative fixer would fix as well.
        // MarkupMode.Allow is needed for the fixed code to declare a diagnostic the fixer could fix.
        test.CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne;
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.FixedCode = """
            using System;

            class {|MA0096:Test|} : IComparable<string>, IComparable<Test>
            {
                public int CompareTo(string other) => throw null;
                public int CompareTo(Test other) => throw null;
                public static bool operator <(Test a, Test b) => throw null;
                public static bool operator <=(Test a, Test b) => throw null;
                public static bool operator >(Test a, Test b) => throw null;
                public static bool operator >=(Test a, Test b) => throw null;
                public static bool operator ==(Test a, Test b) => throw null;
                public static bool operator !=(Test a, Test b) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MA0096_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class {|MA0096:Test|} : IComparable<Test>
            {
                public int CompareTo(Test other) => throw null;
                public override bool Equals(object other) => throw null;
                public override int GetHashCode() => 0;
                public static bool operator <(Test a, Test b) => throw null;
                public static bool operator <=(Test a, Test b) => throw null;
                public static bool operator >(Test a, Test b) => throw null;
                public static bool operator >=(Test a, Test b) => throw null;
                public static bool operator ==(Test a, Test b) => throw null;
                public static bool operator !=(Test a, Test b) => throw null;
            }
            """;
        test.FixedCode = """
            using System;

            class Test : IComparable<Test>, IEquatable<Test>
            {
                public int CompareTo(Test other) => throw null;
                public override bool Equals(object other) => throw null;
                public override int GetHashCode() => 0;
                public static bool operator <(Test a, Test b) => throw null;
                public static bool operator <=(Test a, Test b) => throw null;
                public static bool operator >(Test a, Test b) => throw null;
                public static bool operator >=(Test a, Test b) => throw null;
                public static bool operator ==(Test a, Test b) => throw null;
                public static bool operator !=(Test a, Test b) => throw null;

                public bool Equals(Test other) => CompareTo(other) == 0;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("static public bool CompareTo(Test other)")]
    [InlineData("private bool CompareTo(Test other)")]
    [InlineData("public bool CompareTo(int other)")]
    [InlineData("public int CompareTo(int other)")]
    [InlineData("public void CompareTo(Test other)")]
    [InlineData("public bool CompareTo(Test other)")]
    public Task ClassImplementsNoInterfaceAndProvidesIncompatibleCompareToMethod_NoDiagnosticReported(string methodSignature)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class Test
            {
                {{methodSignature}} => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingOperators()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class {|MA0094:Test|} : IComparable<string>
            {
                public int CompareTo(string other) => throw null;
                public int CompareTo(Test other) => throw null;
            }
            """;

        return test.RunAsync();
    }
}

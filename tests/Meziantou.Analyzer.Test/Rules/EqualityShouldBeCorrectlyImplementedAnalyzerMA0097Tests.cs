using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.EqualityShouldBeCorrectlyImplementedAnalyzer,
    Meziantou.Analyzer.Rules.EqualityShouldBeCorrectlyImplementedFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EqualityShouldBeCorrectlyImplementedAnalyzerMA0097Tests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task BaseClassImplementsOperators()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            abstract class Test : IComparable
            {
                public int CompareTo(object other) => 0;
                public override bool Equals(object? obj) => true;
                public override int GetHashCode() => 0;
                public static bool operator <(Test a, Test b) => true;
                public static bool operator <=(Test a, Test b) => true;
                public static bool operator >(Test a, Test b) => true;
                public static bool operator >=(Test a, Test b) => true;
                public static bool operator ==(Test a, Test b) => true;
                public static bool operator !=(Test a, Test b) => true;
            }

            class InheritedTest : Test // should be ok as the operators are implemented in the base class
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MA0097_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class {|MA0097:Test|} : IComparable<Test>, IEquatable<Test>
            {
                public int CompareTo(Test other) => throw null;
                public bool Equals(Test other) => throw null;
                public override bool Equals(object obj) => throw null;
                public override int GetHashCode() => 0;
            }
            """;
        test.FixedCode = """
            using System;

            class Test : IComparable<Test>, IEquatable<Test>
            {
                public int CompareTo(Test other) => throw null;
                public bool Equals(Test other) => throw null;
                public override bool Equals(object obj) => throw null;
                public override int GetHashCode() => 0;

                public static bool operator <(Test left, Test right) => System.Collections.Generic.Comparer<Test>.Default.Compare(left, right) < 0;
                public static bool operator <=(Test left, Test right) => System.Collections.Generic.Comparer<Test>.Default.Compare(left, right) <= 0;
                public static bool operator >(Test left, Test right) => System.Collections.Generic.Comparer<Test>.Default.Compare(left, right) > 0;
                public static bool operator >=(Test left, Test right) => System.Collections.Generic.Comparer<Test>.Default.Compare(left, right) >= 0;
                public static bool operator ==(Test left, Test right) => System.Collections.Generic.EqualityComparer<Test>.Default.Equals(left, right);
                public static bool operator !=(Test left, Test right) => !System.Collections.Generic.EqualityComparer<Test>.Default.Equals(left, right);
            }
            """;

        return test.RunAsync();
    }
}

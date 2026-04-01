using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EqualityShouldBeCorrectlyImplementedAnalyzerMA0097Tests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<EqualityShouldBeCorrectlyImplementedAnalyzer>()
            .WithCodeFixProvider<EqualityShouldBeCorrectlyImplementedFixer>();
    }

    [Fact]
    public async Task BaseClassImplementsOperators()
    {
        var originalCode = @"using System;
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
";
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MA0097_CodeFix()
    {
        var originalCode = """
            using System;

            class {|MA0097:Test|} : IComparable<Test>, IEquatable<Test>
            {
                public int CompareTo(Test other) => throw null;
                public bool Equals(Test other) => throw null;
                public override bool Equals(object obj) => throw null;
                public override int GetHashCode() => 0;
            }
            """;
        var fixedCode = """
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

        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(fixedCode)
              .ValidateAsync();
    }
}

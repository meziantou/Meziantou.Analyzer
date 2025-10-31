using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EqualityShouldBeCorrectlyImplementedAnalyzerMA0094Tests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<EqualityShouldBeCorrectlyImplementedAnalyzer>();
    }

    [Fact]
    public async Task ClassImplementsNoInterfaceAndProvidesCompatibleCompareToMethod_DiagnosticIsReported()
    {
        var originalCode = """
            using System;
            
            class [|Test|] : IComparable<string>
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

        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AlreadyImplemented()
    {
        var originalCode = """
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

        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MissingIEquatable()
    {
        var originalCode = """
            using System;
            
            class [|Test|] : IComparable<Test>
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

        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("static public bool CompareTo(Test other)")]
    [InlineData("private bool CompareTo(Test other)")]
    [InlineData("public bool CompareTo(int other)")]
    [InlineData("public int CompareTo(int other)")]
    [InlineData("public void CompareTo(Test other)")]
    [InlineData("public bool CompareTo(Test other)")]
    public async Task ClassImplementsNoInterfaceAndProvidesIncompatibleCompareToMethod_NoDiagnosticReported(string methodSignature)
    {
        var originalCode = $@"
class Test
{{
    {methodSignature} => throw null;
}}";
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MissingOperators()
    {
        var originalCode = """
            using System;
            
            class [|Test|] : IComparable<string>
            {
                public int CompareTo(string other) => throw null;
                public int CompareTo(Test other) => throw null;
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }
}

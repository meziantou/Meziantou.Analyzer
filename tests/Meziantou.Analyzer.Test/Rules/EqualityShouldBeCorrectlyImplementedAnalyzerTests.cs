using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.EqualityShouldBeCorrectlyImplementedAnalyzer,
    Meziantou.Analyzer.Rules.EqualityShouldBeCorrectlyImplementedFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EqualityShouldBeCorrectlyImplementedAnalyzerTests
{
    [Fact]
    public Task Test_ClassImplementsNoInterfaceAndProvidesCompatibleEqualsMethod_DiagnosticIsReported()
    {
        var test = new CodeFixTest();
        // Applying this fix reveals MA0095, and the test asserts the result of a single application
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne;
        test.TestCode = """
            class BaseClass {}
            class {|MA0077:Test|} : BaseClass
            {
                public bool Equals(Test other) => throw null;
            }
            """;
        test.FixedCode = """
            class BaseClass {}
            class {|MA0095:Test|} : BaseClass, System.IEquatable<Test>
            {
                public bool Equals(Test other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_StructImplementsNoInterfaceAndProvidesCompatibleEqualsMethod_DiagnosticIsReported()
    {
        var test = new CodeFixTest();
        // Applying this fix reveals MA0095, and the test asserts the result of a single application
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne;
        test.TestCode = """
            struct {|MA0077:Test|}     //  This comment stays
            {
                public bool Equals(Test other) => throw null;
            }
            """;
        test.FixedCode = """
            struct {|MA0095:Test|} : System.IEquatable<Test>     //  This comment stays
            {
                public bool Equals(Test other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RefStruct_CSharp12()
    {
        var test = new CodeFixTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            ref struct Test
            {
                public bool Equals(Test other) => throw null;
            }
            """;

        return test.RunAsync();
    }

#if CSHARP13_OR_GREATER
    [Fact]
    public Task RefStruct_CSharp13()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            ref struct {|MA0077:Test|}
            {
                public bool Equals(Test other) => throw null;
            }
            """;

        return test.RunAsync();
    }
#endif

    [Fact]
    public Task Test_ClassImplementsSystemIEquatableWithTOfWrongTypeButProvidesCompatibleEqualsMethod_DiagnosticIsReported()
    {
        var test = new CodeFixTest();
        // Applying this fix reveals MA0095, and the test asserts the result of a single application
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne;
        test.TestCode = """
            using System;
            class {|MA0077:Test|} : IEquatable<string>
            {
                public bool Equals(Test other) => throw null;
                public bool Equals(string other) => throw null;
            }
            """;
        test.FixedCode = """
            using System;
            class {|MA0095:Test|} : IEquatable<string>, IEquatable<Test>
            {
                public bool Equals(Test other) => throw null;
                public bool Equals(string other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_StructImplementsSystemIEquatableWithTOfWrongTypeButProvidesCompatibleEqualsMethod_DiagnosticIsReported()
    {
        var test = new CodeFixTest();
        // Applying this fix reveals MA0095, and the test asserts the result of a single application
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne;
        test.TestCode = """
            using System;
            struct {|MA0077:Test|} : IEquatable<string>
            {
                public bool Equals(Test other) => throw null;
                public bool Equals(string other) => throw null;
            }
            """;
        test.FixedCode = """
            using System;
            struct {|MA0095:Test|} : IEquatable<string>, IEquatable<Test>
            {
                public bool Equals(Test other) => throw null;
                public bool Equals(string other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_ClassImplementsWrongIEquatableButProvidesCompatibleEqualsMethod_DiagnosticIsReported()
    {
        var test = new CodeFixTest();
        // Applying this fix reveals MA0095, and the test asserts the result of a single application
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne;
        test.TestCode = """
            interface IEquatable<T> { bool Equals(T other); }
            class {|MA0077:Test|} : IEquatable<Test>
            {
                public bool Equals(Test other) => throw null;
            }
            """;
        test.FixedCode = """
            interface IEquatable<T> { bool Equals(T other); }
            class {|MA0095:Test|} : IEquatable<Test>, System.IEquatable<Test>
            {
                public bool Equals(Test other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_StructImplementsWrongIEquatableButProvidesCompatibleEqualsMethod_DiagnosticIsReported()
    {
        var test = new CodeFixTest();
        // Applying this fix reveals MA0095, and the test asserts the result of a single application
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne;
        test.TestCode = """
            interface IEquatable<T> { bool Equals(T other); }
            struct {|MA0077:Test|} : IEquatable<Test>
            {
                public bool Equals(Test other) => throw null;
            }
            """;
        test.FixedCode = """
            interface IEquatable<T> { bool Equals(T other); }
            struct {|MA0095:Test|} : IEquatable<Test>, System.IEquatable<Test>
            {
                public bool Equals(Test other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_ClassImplementsNoInterfaceButProvidesEqualsMethodOnNullableType_DiagnosticIsReported()
    {
        var test = new CodeFixTest();
        // Applying this fix reveals MA0095, and the test asserts the result of a single application
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne;
        test.TestCode = """
            #nullable enable
            class {|MA0077:Test|}
            {
                public bool Equals(Test? other) => throw null;
            }
            """;
        test.FixedCode = """
            #nullable enable
            class {|MA0095:Test|} : System.IEquatable<Test?>
            {
                public bool Equals(Test? other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_ClassImplementsNoInterfaceButProvidesEqualsMethodOnNonNullableType_DiagnosticIsReported()
    {
        var test = new CodeFixTest();
        // Applying this fix reveals MA0095, and the test asserts the result of a single application
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne;
        test.TestCode = """
            #nullable enable
            class {|MA0077:Test|}
            {
                public bool Equals(Test other) => throw null;
            }
            """;
        test.FixedCode = """
            #nullable enable
            class {|MA0095:Test|} : System.IEquatable<Test>
            {
                public bool Equals(Test other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("static public bool Equals(Test other)")]
    [InlineData("private bool Equals(Test other)")]
    [InlineData("public bool Equals(int other)")]
    [InlineData("public int Equals(Test other)")]
    [InlineData("public void Equals(Test other)")]
    [InlineData("public bool EqualsTo(Test other)")]
    public Task Test_ClassImplementsNoInterfaceAndProvidesIncompatibleEqualsMethod_NoDiagnosticReported(string methodSignature)
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            class Test
            {
                {{methodSignature}} => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("static public bool Equals(Test other)")]
    [InlineData("private bool Equals(Test other)")]
    [InlineData("public bool Equals(int other)")]
    [InlineData("public int Equals(Test other)")]
    [InlineData("public void Equals(Test other)")]
    [InlineData("public bool EqualsTo(Test other)")]
    public Task Test_StructImplementsNoInterfaceAndProvidesIncompatibleEqualsMethod_NoDiagnosticReported(string methodSignature)
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            struct Test
            {
                {{methodSignature}} => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_ClassImplementsSystemIEquatableWithTOfRightType_NoDiagnosticReported()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;
            class Test : IEquatable<Test>
            {
                public override bool Equals(object o) => throw null;
                public bool Equals(Test other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_StructImplementsSystemIEquatableWithTOfRightType_NoDiagnosticReported()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;
            struct Test : IEquatable<Test>
            {
                public override bool Equals(object o) => throw null;
                public bool Equals(Test other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_InterfaceDoesNotInheritFromSystemIEquatableButProvidesCompatibleEqualsMethod_NoDiagnosticReported()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            public interface ITest
            {
                bool Equals(ITest other);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClassImplementsNoInterfaceAndProvidesCompatibleCompareToMethod_DiagnosticIsReported()
    {
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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

    [Fact]
    public Task DirectImplementation_WithoutEqualsObject_ShouldTrigger()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;

            public sealed class {|MA0095:TriggersMA0095AndCA1067|} : IEquatable<TriggersMA0095AndCA1067>
            {
                public bool Equals(TriggersMA0095AndCA1067? other) => true;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MA0095_CodeFix()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;

            public sealed class {|MA0095:Test|} : IEquatable<Test>
            {
                public bool Equals(Test? other) => true;
            }
            """;
        test.FixedCode = """
            using System;

            public sealed class Test : IEquatable<Test>
            {
                public bool Equals(Test? other) => true;
                public override bool Equals(object obj) => obj is Test other && Equals(other);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DirectImplementation_WithEqualsObject_ShouldNotTrigger()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;

            public sealed class Test : IEquatable<Test>
            {
                public bool Equals(Test? other) => true;
                public override bool Equals(object? obj) => true;
                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CRTP_WithoutEqualsObject_ShouldNotTrigger()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;

            public abstract class Crtp<T> : IEquatable<T> where T : Crtp<T>
            {
                public bool Equals(T? other) => true;
            }

            public sealed class TriggersMA0095Only : Crtp<TriggersMA0095Only>;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CRTP_WithEqualsObjectInBase_ShouldNotTrigger()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;

            public abstract class Crtp<T> : IEquatable<T> where T : Crtp<T>
            {
                public bool Equals(T? other) => true;
                public override bool Equals(object? obj) => true;
                public override int GetHashCode() => 0;
            }

            public sealed class DerivedClass : Crtp<DerivedClass>;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InheritedIEquatable_WithDirectImplementationToo_ShouldTrigger()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;

            public abstract class Base : IEquatable<Base>
            {
                public bool Equals(Base? other) => true;
                public override bool Equals(object? obj) => true;
                public override int GetHashCode() => 0;
            }

            public sealed class {|MA0095:Derived|} : Base, IEquatable<Derived>
            {
                public bool Equals(Derived? other) => true;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Struct_DirectImplementation_WithoutEqualsObject_ShouldTrigger()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System;

            public struct {|MA0095:TestStruct|} : IEquatable<TestStruct>
            {
                public bool Equals(TestStruct other) => true;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task BaseClassImplementsOperators()
    {
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.EqualityShouldBeCorrectlyImplementedAnalyzer,
    Meziantou.Analyzer.Rules.EqualityShouldBeCorrectlyImplementedFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EqualityShouldBeCorrectlyImplementedAnalyzerMA0077Tests
{
    // This class covers MA0077 only: fixing it adds IEquatable<T>, which makes MA0095 report on the result,
    // and the iterative fixer would apply that fix as well
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add(RuleIdentifiers.ClassWithEqualsTShouldOverrideEqualsObject);
        return test;
    }

    [Fact]
    public Task Test_ClassImplementsNoInterfaceAndProvidesCompatibleEqualsMethod_DiagnosticIsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            class BaseClass {}
            class {|MA0077:Test|} : BaseClass
            {
                public bool Equals(Test other) => throw null;
            }
            """;
        test.FixedCode = """
            class BaseClass {}
            class Test : BaseClass, System.IEquatable<Test>
            {
                public bool Equals(Test other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_StructImplementsNoInterfaceAndProvidesCompatibleEqualsMethod_DiagnosticIsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            struct {|MA0077:Test|}     //  This comment stays
            {
                public bool Equals(Test other) => throw null;
            }
            """;
        test.FixedCode = """
            struct Test : System.IEquatable<Test>     //  This comment stays
            {
                public bool Equals(Test other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RefStruct_CSharp12()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
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
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp13;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
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
        var test = CreateTest();
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
            class Test : IEquatable<string>, IEquatable<Test>
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
        var test = CreateTest();
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
            struct Test : IEquatable<string>, IEquatable<Test>
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
        var test = CreateTest();
        test.TestCode = """
            interface IEquatable<T> { bool Equals(T other); }
            class {|MA0077:Test|} : IEquatable<Test>
            {
                public bool Equals(Test other) => throw null;
            }
            """;
        test.FixedCode = """
            interface IEquatable<T> { bool Equals(T other); }
            class Test : IEquatable<Test>, System.IEquatable<Test>
            {
                public bool Equals(Test other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_StructImplementsWrongIEquatableButProvidesCompatibleEqualsMethod_DiagnosticIsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            interface IEquatable<T> { bool Equals(T other); }
            struct {|MA0077:Test|} : IEquatable<Test>
            {
                public bool Equals(Test other) => throw null;
            }
            """;
        test.FixedCode = """
            interface IEquatable<T> { bool Equals(T other); }
            struct Test : IEquatable<Test>, System.IEquatable<Test>
            {
                public bool Equals(Test other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_ClassImplementsNoInterfaceButProvidesEqualsMethodOnNullableType_DiagnosticIsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            #nullable enable
            class {|MA0077:Test|}
            {
                public bool Equals(Test? other) => throw null;
            }
            """;
        test.FixedCode = """
            #nullable enable
            class Test : System.IEquatable<Test?>
            {
                public bool Equals(Test? other) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_ClassImplementsNoInterfaceButProvidesEqualsMethodOnNonNullableType_DiagnosticIsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            #nullable enable
            class {|MA0077:Test|}
            {
                public bool Equals(Test other) => throw null;
            }
            """;
        test.FixedCode = """
            #nullable enable
            class Test : System.IEquatable<Test>
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
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
        var test = CreateTest();
        test.TestCode = """
            public interface ITest
            {
                bool Equals(ITest other);
            }
            """;

        return test.RunAsync();
    }
}

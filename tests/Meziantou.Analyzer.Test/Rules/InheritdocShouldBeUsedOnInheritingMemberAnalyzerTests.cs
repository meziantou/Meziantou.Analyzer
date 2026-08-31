using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.InheritdocShouldBeUsedOnInheritingMemberAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class InheritdocShouldBeUsedOnInheritingMemberAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task ReportDiagnostic_MethodIsNotOverrideOrInterfaceImplementation()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// {|MA0196:<inheritdoc />|}
                public void M() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_PropertyIsNotOverrideOrInterfaceImplementation()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// {|MA0196:<inheritdoc />|}
                public int P { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_ConstructorIsNotOverrideOrInterfaceImplementation()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// {|MA0196:<inheritdoc />|}
                public Sample(int value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_ConstructorHasSameSignatureAsBaseConstructor()
    {
        var test = CreateTest();
        test.TestCode = """
            class BaseType
            {
                public BaseType(int value) { }
            }

            class Sample : BaseType
            {
                /// <inheritdoc />
                public Sample(int value) : this() { }

                private Sample() : base(0) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_ConstructorHasSameSignatureAsBaseParameterlessConstructor()
    {
        var test = CreateTest();
        test.TestCode = """
            class BaseType
            {
                public BaseType() { }
            }

            class Sample : BaseType
            {
                /// <inheritdoc />
                public Sample() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_ConstructorDoesNotMatchBaseConstructorSignature()
    {
        var test = CreateTest();
        test.TestCode = """
            class BaseType
            {
                public BaseType(int value) { }
            }

            class Sample : BaseType
            {
                /// {|MA0196:<inheritdoc />|}
                public Sample(string value) : base(0) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenInheritdocIsOnTypeWithPrimaryConstructor()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <inheritdoc />
            public class Sample() { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_MethodIsOverride()
    {
        var test = CreateTest();
        test.TestCode = """
            class BaseType
            {
                public virtual void M() { }
            }

            class Sample : BaseType
            {
                /// <inheritdoc />
                public override void M() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_MethodIsInterfaceImplementation()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
                void M();
            }

            class Sample : ITest
            {
                /// <inheritdoc />
                public void M() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_MethodIsExplicitInterfaceImplementation()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
                void M();
            }

            class Sample : ITest
            {
                /// <inheritdoc />
                void ITest.M() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenCrefIsPresent()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// <inheritdoc cref="object.ToString" />
                public void M() { }
            }
            """;

        return test.RunAsync();
    }
}

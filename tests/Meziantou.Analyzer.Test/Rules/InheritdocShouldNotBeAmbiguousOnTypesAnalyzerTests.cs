using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.InheritdocShouldNotBeAmbiguousOnTypesAnalyzer,
    Meziantou.Analyzer.Rules.InheritdocShouldNotBeUsedOnTypesFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class InheritdocShouldNotBeAmbiguousOnTypesAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task ReportDiagnostic_MA0198_WhenMultipleDeclaredInterfacesArePresentAndNoBaseType()
    {
        var test = CreateTest();
        test.TestCode = """
            interface IInterface1
            {
            }

            interface IInterface2
            {
            }

            /// {|MA0198:<inheritdoc />|}
            class Sample : IInterface1, IInterface2
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_MA0198_EmptyElement_FirstInterface()
    {
        var test = CreateTest();
        test.TestCode = """
            interface IInterface1
            {
            }

            interface IInterface2
            {
            }

            /// {|MA0198:<inheritdoc />|}
            class Sample : IInterface1, IInterface2
            {
            }
            """;
        test.FixedCode = """
            interface IInterface1
            {
            }

            interface IInterface2
            {
            }

            /// <inheritdoc cref="IInterface1" />
            class Sample : IInterface1, IInterface2
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_MA0198_EmptyElement_SecondInterface()
    {
        var test = CreateTest();
        test.TestCode = """
            interface IInterface1
            {
            }

            interface IInterface2
            {
            }

            /// {|MA0198:<inheritdoc />|}
            class Sample : IInterface1, IInterface2
            {
            }
            """;
        test.CodeActionIndex = 1;
        test.FixedCode = """
            interface IInterface1
            {
            }

            interface IInterface2
            {
            }

            /// <inheritdoc cref="IInterface2" />
            class Sample : IInterface1, IInterface2
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_MA0198_XmlElement()
    {
        var test = CreateTest();
        test.TestCode = """
            interface IInterface1
            {
            }

            interface IInterface2
            {
            }

            /// {|MA0198:<inheritdoc>|}</inheritdoc>
            class Sample : IInterface1, IInterface2
            {
            }
            """;
        test.CodeActionIndex = 1;
        test.FixedCode = """
            interface IInterface1
            {
            }

            interface IInterface2
            {
            }

            /// <inheritdoc cref="IInterface2"></inheritdoc>
            class Sample : IInterface1, IInterface2
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenSingleDeclaredInterfaceIsPresent()
    {
        var test = CreateTest();
        test.TestCode = """
            interface IInterface1
            {
            }

            /// <inheritdoc />
            class Sample : IInterface1
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenBaseTypeIsPresent()
    {
        var test = CreateTest();
        test.TestCode = """
            class BaseClass
            {
            }

            interface IInterface1
            {
            }

            interface IInterface2
            {
            }

            /// <inheritdoc />
            class Sample : BaseClass, IInterface1, IInterface2
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenCrefIsPresent()
    {
        var test = CreateTest();
        test.TestCode = """
            interface IInterface1
            {
            }

            interface IInterface2
            {
            }

            /// <inheritdoc cref="T:IInterface1" />
            class Sample : IInterface1, IInterface2
            {
            }
            """;

        return test.RunAsync();
    }
}

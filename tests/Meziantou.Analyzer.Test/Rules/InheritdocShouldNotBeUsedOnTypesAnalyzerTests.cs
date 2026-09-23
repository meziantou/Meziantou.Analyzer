using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.InheritdocShouldNotBeUsedOnTypesAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class InheritdocShouldNotBeUsedOnTypesAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task ReportDiagnostic_MA0197_WhenBaseTypeIsPresent()
    {
        var test = CreateTest();
        test.TestCode = """
            class BaseType
            {
            }

            /// {|MA0197:<inheritdoc />|}
            class Sample : BaseType
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_MA0197_WhenSingleDeclaredInterfaceIsPresent()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
            }

            /// {|MA0197:<inheritdoc />|}
            class Sample : ITest
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_MA0197_WhenDeclaredInterfaceInheritsMultipleInterfaces()
    {
        var test = CreateTest();
        test.TestCode = """
            interface IInterface1
            {
            }

            interface IInterface2
            {
            }

            interface ICompositeInterface : IInterface1, IInterface2
            {
            }

            /// {|MA0197:<inheritdoc />|}
            class Sample : ICompositeInterface
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
            /// <inheritdoc cref="object" />
            class Sample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenCrefIsPresentOnXmlElement()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <inheritdoc cref="object"></inheritdoc>
            class Sample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenUsedOnMember()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// <inheritdoc />
                public override string ToString() => base.ToString();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_MA0197_WhenRecordHasNoDeclaredInterface()
    {
        // The compiler implements IEquatable<Sample> on the record, so it is the only source of the documentation
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0197:<inheritdoc />|}
            record Sample;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_MA0197_WhenRecordDeclaresEquatableInterface()
    {
        // The compiler merges the declared IEquatable<Sample> with the one it implements on the record
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0197:<inheritdoc />|}
            record Sample : System.IEquatable<Sample>;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenRecordHasDeclaredInterface()
    {
        // The interfaces of the record are ITest and IEquatable<Sample>, which is reported by MA0198
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
            }

            /// <inheritdoc />
            record Sample : ITest;
            """;

        return test.RunAsync();
    }
}

using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.InheritdocShouldHaveSourceOnTypesAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class InheritdocShouldHaveSourceOnTypesAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task ReportDiagnostic_MA0199_WhenNoBaseTypeAndNoDeclaredInterface()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0199:<inheritdoc />|}
            class Sample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_MA0199_WhenInterfaceHasNoBaseInterface()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0199:<inheritdoc />|}
            interface ITest
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_ForEachPartialDeclaration()
    {
        var test = CreateTest();
        test.TestCode = """
            /// {|MA0199:<inheritdoc />|}
            partial class Sample
            {
            }

            /// {|MA0199:<inheritdoc />|}
            partial class Sample
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

            /// <inheritdoc />
            class Sample : BaseClass
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
    public Task NoDiagnostic_WhenInterfaceInheritsAnotherInterface()
    {
        var test = CreateTest();
        test.TestCode = """
            interface IBase
            {
            }

            /// <inheritdoc />
            interface IChild : IBase
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenRecordInheritsBaseRecord()
    {
        var test = CreateTest();
        test.TestCode = """
            record BaseRecord;

            /// <inheritdoc />
            record Sample : BaseRecord;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenStructImplementsInterface()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
            }

            /// <inheritdoc />
            struct Sample : ITest
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenRecordStructImplementsInterface()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
            }

            /// <inheritdoc />
            record struct Sample : ITest;
            """;

        return test.RunAsync();
    }
}

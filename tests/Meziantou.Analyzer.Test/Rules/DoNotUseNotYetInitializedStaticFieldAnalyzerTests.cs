using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseNotYetInitializedStaticFieldAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseNotYetInitializedStaticFieldAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task ReportDiagnostic_WhenReferencingLaterFieldInSamePart()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static readonly bool[] Values = new[] { [|P1|], [|P2|] };
                private static readonly bool P1 = true;
                private static readonly bool P2 = false;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenReferencingEarlierFieldInSamePart()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static readonly bool P1 = true;
                private static readonly bool P2 = false;
                private static readonly bool[] Values = new[] { P1, P2 };
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_WhenReferencingFieldFromAnotherPartialDeclaration()
    {
        var test = CreateTest();
        test.TestCode = """
            partial class Sample
            {
                private static readonly bool P1 = true;
            }

            partial class Sample
            {
                private static readonly bool[] Values = new[] { [|P1|] };
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenPartialDeclarationsOnlyReferenceEarlierFieldsInSamePart()
    {
        var test = CreateTest();
        test.TestCode = """
            partial class Sample
            {
                private static readonly int P1 = 1;
                private static readonly int[] Values1 = new[] { P1 };
            }

            partial class Sample
            {
                private static readonly int P2 = 2;
                private static readonly int[] Values2 = new[] { P2 };
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenReferencingFieldFromAnotherPartialDeclarationWithoutInitializer()
    {
        var test = CreateTest();
        test.TestCode = """
            partial class Sample
            {
                private static readonly int Other;
            }

            partial class Sample
            {
                private static readonly int Value = Other;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenReferencedFieldHasNoInitializer()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static readonly int Value = Other;
                private static readonly int Other;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenReferenceIsInNameof()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static readonly string Value = nameof(Other);
                private static readonly int Other = 42;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_WhenReferencedFieldIsOnlyAssignedInStaticConstructor()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static readonly int Other;
                private static readonly int Value = {|#0:Other|};

                static Sample()
                {
                    Other = 42;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.DoNotUseNotYetInitializedStaticField, DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Static field 'Other' may not be initialized yet because it is assigned in the static constructor, which runs after the static field initializers"));

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_WhenReferencedFieldAssignedInStaticConstructorIsUsedInMethodCall()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static readonly string Path;
                private static readonly string Value = Compute([|Path|]);

                static Sample()
                {
                    Path = "path";
                }

                private static string Compute(string? path = null) => path ?? "default";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_WhenReferencedFieldIsAssignedInStaticConstructorOfAnotherPartialDeclaration()
    {
        var test = CreateTest();
        test.TestCode = """
            partial class Sample
            {
                private static readonly int Value = [|Other|];
            }

            partial class Sample
            {
                private static readonly int Other;

                static Sample()
                {
                    Other = 42;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenFieldIsOnlyReadInStaticConstructor()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static readonly int Other;
                private static int Value;

                static Sample()
                {
                    Value = Other;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenReferencedFieldIsAssignedInAnInstanceConstructor()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static int Other;
                private static readonly int Value = Other;

                public Sample()
                {
                    Other = 42;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenReferencedFieldIsAssignedInAStaticConstructorLambda()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static int Other;
                private static readonly int Value = Other;

                static Sample()
                {
                    System.Action action = () => Other = 42;
                    action();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportDiagnostic_WhenTypeIsAnInterface()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ISample
            {
                private static readonly int Other;
                private static readonly int Value = [|Other|];

                static ISample()
                {
                    Other = 42;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_ForEnumMembers()
    {
        var test = CreateTest();
        test.TestCode = """
            enum Sample
            {
                A = 1,
                B = A + 1,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_WhenReferenceIsInLambda()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private static readonly System.Func<int> ValueFactory = () => Other;
                private static readonly int Other = 42;
            }
            """;

        return test.RunAsync();
    }
}

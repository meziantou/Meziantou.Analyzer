using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DeclareTypesInNamespacesAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DeclareTypesInNamespacesAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task InNamespace_IsValid()
    {
        var test = CreateTest();
        test.TestCode = """
            namespace Test
            {
                class Sample
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotInNamespace_IsInvalid()
    {
        var test = CreateTest();
        test.TestCode = """
            class {|MA0047:Sample|}
            {
                class Nested { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TopLevelProgram_9()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp9;
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            System.Console.WriteLine();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TopLevelProgram_10()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            System.Console.WriteLine();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TopLevelProgram_10_partial()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            System.Console.WriteLine();

            public partial class Program { }
            """;

        return test.RunAsync();
    }
}

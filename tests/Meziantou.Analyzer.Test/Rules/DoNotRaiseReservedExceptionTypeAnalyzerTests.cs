#pragma warning disable CA1030 // Use events where appropriate

using Microsoft.CodeAnalysis;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotRaiseReservedExceptionTypeAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotRaiseReservedExceptionTypeAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task RaiseNotReservedException_ShouldNotReportErrorAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class TestAttribute
            {
                void Test()
                {
                    throw new Exception();
                    throw new ArgumentException();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RaiseReservedException_ShouldReportErrorAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class TestAttribute
            {
                void Test()
                {
                    {|#0:throw new IndexOutOfRangeException();|}
                }
            }
            """;
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(RuleIdentifiers.DoNotRaiseReservedExceptionType, DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithMessage("'System.IndexOutOfRangeException' is a reserved exception type"));

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowNull()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class TestAttribute
            {
                void Test()
                {
                    throw null;
                }
            }
            """;

        return test.RunAsync();
    }
}

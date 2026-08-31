using Microsoft.CodeAnalysis;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotImplicitlyConvertDateTimeToDateTimeOffsetAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public class DoNotCompareDateTimeWithDateTimeOffsetAnalyzerTests_MA0133
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.DisabledDiagnostics.Add(RuleIdentifiers.DoNotImplicitlyConvertDateTimeToDateTimeOffset);
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task ImplicitConversion_BinaryOperation_Subtract_UtcNow()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            _ = {|MA0133:DateTime.UtcNow|} - DateTimeOffset.UtcNow;
            """;

        return test.RunAsync();
    }
}

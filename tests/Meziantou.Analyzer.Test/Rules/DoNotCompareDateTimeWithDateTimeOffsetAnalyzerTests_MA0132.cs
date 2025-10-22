using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public class DoNotCompareDateTimeWithDateTimeOffsetAnalyzerTests_MA0132

{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithOutputKind(OutputKind.ConsoleApplication)
            .WithAnalyzer<DoNotImplicitlyConvertDateTimeToDateTimeOffsetAnalyzer>(id: "MA0132");
    }

    [Fact]
    public async Task ImplicitConversion_BinaryOperation_Subtract_UtcNow()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                _ = DateTime.UtcNow - DateTimeOffset.UtcNow;
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ImplicitConversion_BinaryOperation_Subtract()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                _ = [|default(DateTime)|] - DateTimeOffset.UtcNow;
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitConversion_BinaryOperation_Subtract()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                _ = (DateTimeOffset)default(DateTime) - DateTimeOffset.UtcNow;
                """)
            .ValidateAsync();
    }
}

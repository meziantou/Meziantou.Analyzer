using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public class DoNotCompareDateTimeWithDateTimeOffsetAnalyzerTests_MA0133
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithOutputKind(OutputKind.ConsoleApplication)
            .WithAnalyzer<DoNotImplicitlyConvertDateTimeToDateTimeOffsetAnalyzer>(id: "MA0133");
    }

    [Fact]
    public async Task ImplicitConversion_BinaryOperation_Subtract_UtcNow()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                _ = [|DateTime.UtcNow|] - DateTimeOffset.UtcNow;
                """)
            .ValidateAsync();
    }
}

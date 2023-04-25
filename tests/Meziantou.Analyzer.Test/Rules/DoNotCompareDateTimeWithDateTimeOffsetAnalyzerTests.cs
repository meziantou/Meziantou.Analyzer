using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public class DoNotCompareDateTimeWithDateTimeOffsetAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithOutputKind(OutputKind.ConsoleApplication)
            .WithAnalyzer<DoNotImplicitlyConvertDateTimeToDateTimeOffsetAnalyzer>();
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

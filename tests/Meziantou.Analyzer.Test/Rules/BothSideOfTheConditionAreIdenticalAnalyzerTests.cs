using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class BothSideOfTheConditionAreIdenticalAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithAnalyzer<BothSideOfTheConditionAreIdenticalAnalyzer>();
    }

    [Theory]
    [InlineData("a == b")]
    [InlineData("a != b")]
    [InlineData("a & b")]
    [InlineData("a && b")]
    [InlineData("a | b")]
    [InlineData("a || b")]
    [InlineData("a is false")]
    [InlineData("a is true")]
    [InlineData("a is false or true")]
    [InlineData("a is false and not true")]
    public async Task DifferentCode(string expression)
    {
        await CreateProjectBuilder()
                .WithSourceCode($$"""
                    var a = false;
                    var b = false;
                    var c = 0;
                    _ = {{expression}};
                    """)
                .ValidateAsync();
    }

    [Theory]
    [InlineData("[|a == a|]")]
    [InlineData("[|a != a|]")]
    [InlineData("[|a & a|]")]
    [InlineData("[|a && a|]")]
    [InlineData("[|a | a|]")]
    [InlineData("[|a || a|]")]
    [InlineData("a is [|true or true|]")]
    [InlineData("a is [|true and true|]")]
    public async Task SameCode(string expression)
    {
        await CreateProjectBuilder()
                .WithSourceCode($$"""
                    var a = false;
                    var b = false;
                    var c = 0;
                    _ = {{expression}};
                    """)
                .ValidateAsync();
    }

}

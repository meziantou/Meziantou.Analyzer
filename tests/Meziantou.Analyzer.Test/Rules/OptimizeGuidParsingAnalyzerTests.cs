using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeGuidParsingAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<OptimizeGuidCreationAnalyzer>()
            .WithCodeFixProvider<OptimizeGuidCreationFixer>()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication);
    }

    [Fact]
    public async Task CtorConstantString()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                 _ = [|new System.Guid("10752bc4-c151-50f5-f27b-df92d8af5a61")|];
                 """)
              .ShouldFixCodeWith("""
                 _ = new System.Guid(0x10752bc4, 0xc151, 0x50f5, 0xf2, 0x7b, 0xdf, 0x92, 0xd8, 0xaf, 0x5a, 0x61) /* 10752bc4-c151-50f5-f27b-df92d8af5a61 */;
                 """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ParseConstantString()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                 _ = [|System.Guid.Parse("10752BC4-C151-50F5-F27B-DF92D8AF5A61")|];
                 """)
              .ShouldFixCodeWith("""
                 _ = new System.Guid(0x10752BC4, 0xC151, 0x50F5, 0xF2, 0x7B, 0xDF, 0x92, 0xD8, 0xAF, 0x5A, 0x61) /* 10752BC4-C151-50F5-F27B-DF92D8AF5A61 */;
                 """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ParseNonConstantString()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                 var value = "10752BC4-C151-50F5-F27B-DF92D8AF5A61";
                 _ = System.Guid.Parse(value);
                 """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ParseInvalidGuid()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                 _ = System.Guid.Parse("dummy");
                 """)
              .ValidateAsync();
    }
}

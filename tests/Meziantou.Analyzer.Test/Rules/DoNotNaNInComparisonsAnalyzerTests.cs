using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotNaNInComparisonsAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotNaNInComparisonsAnalyzer>()
            .WithCodeFixProvider<DoNotNaNInComparisonsFixer>();
    }

    [Fact]
    public async Task Comparisons()
    {
        const string SourceCode = """
            class Test
            {
                void A()
                {
                    _ = 1d == 0d;
                    _ = 1d != 0d;
                    _ = 0d == [|double.NaN|];
                    _ = 0d != [|double.NaN|];
                    _ = [|double.NaN|] == 0d;
                    _ = [|double.NaN|] != 0d;

                    _ = 1f == 0f;
                    _ = 1f != 0f;
                    _ = 0f == [|float.NaN|];
                    _ = 0f != [|float.NaN|];
                    _ = [|float.NaN|] == 0f;
                    _ = [|float.NaN|] != 0f;

                    _ = (double)[|float.NaN|] != 1f;

                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Comparisons_CodeFix()
    {
        const string SourceCode = """
            class Test
            {
                void A(double value)
                {
                    _ = value == [|double.NaN|];
                }
            }
            """;

        const string Fix = """
            class Test
            {
                void A(double value)
                {
                    _ = double.IsNaN(value);
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }
}

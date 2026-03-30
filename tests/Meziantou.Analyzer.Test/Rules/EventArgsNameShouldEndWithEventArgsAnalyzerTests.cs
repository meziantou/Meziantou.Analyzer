using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EventArgsNameShouldEndWithEventArgsAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<EventArgsNameShouldEndWithEventArgsAnalyzer>()
            .WithCodeFixProvider<TypeNameShouldEndWithSuffixFixer>();
    }

    [Fact]
    public async Task NameEndsWithEventArgs()
    {
        const string SourceCode = """
            class CustomEventArgs : System.EventArgs
            {
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NameDoesNotEndWithEventArgs()
    {
        const string SourceCode = """
            class [|CustomArgs|] : System.EventArgs
            {
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NameDoesNotEndWithEventArgs_CodeFix()
    {
        const string SourceCode = """
            class [|CustomArgs|] : System.EventArgs
            {
            }
            """;

        const string Fix = """
            class CustomArgsEventArgs : System.EventArgs
            {
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }
}

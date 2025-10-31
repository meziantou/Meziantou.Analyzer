using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ArgumentExceptionShouldSpecifyArgumentNameAnalyzer_UseNameofTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<ArgumentExceptionShouldSpecifyArgumentNameAnalyzer>(id: "MA0043")
            .WithCodeFixProvider<ArgumentExceptionShouldSpecifyArgumentNameFixer>();
    }

    [Fact]
    public async Task Property()
    {
        const string SourceCode = """
            class Sample
            {
                string Prop
                {
                    get { throw null; }
                    set { throw new System.ArgumentNullException([||]""value""); }
                }
            }
            """;

        const string CodeFix = """
            class Sample
            {
                string Prop
                {
                    get { throw null; }
                    set { throw new System.ArgumentNullException(nameof(value)); }
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Method()
    {
        const string SourceCode = """
            class Sample
            {
                string M(string arg0)
                {
                    throw new System.ArgumentNullException([||]""arg0"");
                }
            }
            """;

        const string CodeFix = """
            class Sample
            {
                string M(string arg0)
                {
                    throw new System.ArgumentNullException(nameof(arg0));
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Operator()
    {
        const string SourceCode = """
            class Sample
            {
                public static Sample operator +(Sample first, Sample second)
                {
                    throw new System.ArgumentNullException([||]""first"");
                }
            }
            """;

        const string CodeFix = """
            class Sample
            {
                public static Sample operator +(Sample first, Sample second)
                {
                    throw new System.ArgumentNullException(nameof(first));
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }
}

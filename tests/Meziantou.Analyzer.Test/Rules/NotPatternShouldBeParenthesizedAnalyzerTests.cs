using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class NotPatternShouldBeParenthesizedAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
        => new ProjectBuilder()
            .WithAnalyzer<NotPatternShouldBeParenthesizedAnalyzer>()
            .WithCodeFixProvider<NotPatternShouldBeParenthesizedCodeFixer>()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication);

    [Fact]
    public async Task Not_Null()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                string a = default;
                _ = a is not null;
                """)
              .ValidateAsync();

    [Fact]
    public async Task Not_Null_Or_Empty()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                string a = default;
                _ = a is [|not null|] or "";
                """)
              .ShouldFixCodeWith(index: 0, """
                string a = default;
                _ = a is (not null) or "";
                """)
              .ValidateAsync();

    [Fact]
    public async Task Not_Null_And_Empty()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                string a = default;
                _ = a is not null and "";
                """)
              .ValidateAsync();

    [Fact]
    public async Task Not_Or_GreaterThan()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                int a = default;
                _ = a is [|not 1|] or > 2;
                """)
              .ShouldFixCodeWith(index: 0, """
                int a = default;
                _ = a is (not 1) or > 2;
                """)
              .ValidateAsync();

    [Fact]
    public async Task Parentheses_Not_Or_GreaterThan()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                int a = 1;
                _ = a is (not 1) or > 2;
                """)
              .ValidateAsync();

    [Fact]
    public async Task GreaterThan_Or_Not()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                int a = 1;
                _ = a is 1 or not (< 0);
                """)
              .ValidateAsync();

    [Fact]
    public async Task GreaterThan_Or_Not_Or_Not()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                int a = 1;
                _ = a is 1 or not < 0 or not > 1;
                """)
              .ValidateAsync();

    [Fact]
    public async Task Not_Many_or_Fix1()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                int a = 1;
                _ = a is [|not 1|] or 2 or 3 or 4;
                """)
              .ShouldFixCodeWith(index: 0, """
                int a = 1;
                _ = a is (not 1) or 2 or 3 or 4;
                """)
              .ValidateAsync();

    [Fact]
    public async Task Not_Many_or_Fix2()
        => await CreateProjectBuilder()
              .WithSourceCode("""
                int a = 1;
                _ = a is [|not 1|] or 2 or 3 or 4;
                """)
              .ShouldFixCodeWith(index: 1, """
                int a = 1;
                _ = a is not (1 or 2 or 3 or 4);
                """)
              .ValidateAsync();
}

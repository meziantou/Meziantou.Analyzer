using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class MakeInterpolatedStringAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
        => new ProjectBuilder()
            .WithAnalyzer<MakeInterpolatedStringAnalyzer>()
            .WithCodeFixProvider<MakeInterpolatedStringFixer>()
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication);

    [Fact]
    public Task SimpleString()
        => CreateProjectBuilder()
              .WithSourceCode("""
                   _ = [|"test"|];
                   """)
              .ShouldFixCodeWith("""
                   _ = $"test";
                   """)
              .ValidateAsync();

    [Fact]
    public Task VerbatimString()
        => CreateProjectBuilder()
              .WithSourceCode("""
                   _ = [|@"test"|];
                   """)
              .ShouldFixCodeWith("""
                   _ = $@"test";
                   """)
              .ValidateAsync();

    [Fact]
    public Task InterpolatedString()
        => CreateProjectBuilder()
              .WithSourceCode("""
                   _ = $"test{42}";
                   """)
              .ValidateAsync();

    [Fact]
    public Task InterpolatedVerbatimString()
        => CreateProjectBuilder()
              .WithSourceCode("""
                   _ = $@"test{42}";
                   """)
              .ValidateAsync();

#if CSHARP10_OR_GREATER
    [Fact]
    public Task RawString()
        => CreateProjectBuilder()
              .WithSourceCode("""""
                   _ = """test{42}""";
                   """"")
              .ValidateAsync();
#endif

    [Fact]
    public Task SimpleStringWithOpenAndCloseCurlyBraces()
        => CreateProjectBuilder()
              .WithSourceCode("""
                   _ = [|"test{0}"|];
                   """)
               .ShouldFixCodeWith("""
                   _ = $"test{0}";
                   """)
              .ValidateAsync();

    [Fact]
    public Task SimpleStringWithOpenCurlyBrace()
        => CreateProjectBuilder()
              .WithSourceCode("""
                   _ = [|"test{0"|];
                   """)
               .ShouldFixCodeWith("""
                   _ = $"test{0";
                   """)
              .WithNoFixCompilation()
              .ValidateAsync();

    [Fact]
    public Task VerbatimStringWithOpenAndCloseCurlyBraces()
        => CreateProjectBuilder()
              .WithSourceCode("""
                   _ = [|@"test{0}"|];
                   """)
               .ShouldFixCodeWith("""
                   _ = $@"test{0}";
                   """)
              .ValidateAsync();

    [Fact]
    public Task VerbatimStringWithOpenCurlyBrace()
        => CreateProjectBuilder()
              .WithSourceCode("""
                   _ = [|@"test{0"|];
                   """)
               .ShouldFixCodeWith("""
                   _ = $@"test{0";
                   """)
              .WithNoFixCompilation()
              .ValidateAsync();

}

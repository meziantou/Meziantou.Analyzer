using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis.CSharp;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RemoveUnnecessaryBracesInTypeDeclarationAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<RemoveUnnecessaryBracesInTypeDeclarationAnalyzer>()
            .WithCodeFixProvider<RemoveUnnecessaryBracesInTypeDeclarationFixer>();
    }

    [Fact]
    public async Task PositionalRecord_WithEmptyBraces()
    {
        await CreateProjectBuilder()
            .WithLanguageVersion(LanguageVersion.CSharp9)
            .WithSourceCode("""
                public record Foo(string Value1, string Value2) [|{|]}
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task PositionalRecord_CodeFix()
    {
        await CreateProjectBuilder()
            .WithLanguageVersion(LanguageVersion.CSharp9)
            .WithSourceCode("""
                public record Foo(string Value1, string Value2) [|{|]}
                """)
            .ShouldFixCodeWith("""
                public record Foo(string Value1, string Value2);
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task PositionalRecord_WithSemicolon()
    {
        await CreateProjectBuilder()
            .WithLanguageVersion(LanguageVersion.CSharp9)
            .WithSourceCode("""
                public record Foo(string Value1, string Value2);
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task PositionalRecord_WithMember()
    {
        await CreateProjectBuilder()
            .WithLanguageVersion(LanguageVersion.CSharp9)
            .WithSourceCode("""
                public record Foo(string Value1, string Value2)
                {
                    public string Value3 { get; init; } = "";
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task PositionalRecord_WithComment()
    {
        await CreateProjectBuilder()
            .WithLanguageVersion(LanguageVersion.CSharp9)
            .WithSourceCode("""
                public record Foo(string Value1, string Value2)
                {
                    // Keep this comment
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task RecordWithoutParameterList()
    {
        await CreateProjectBuilder()
            .WithLanguageVersion(LanguageVersion.CSharp9)
            .WithSourceCode("""
                public record Foo [|{|]}
                """)
            .ShouldFixCodeWith("""
                public record Foo;
                """)
            .ValidateAsync();
    }

#if CSHARP12_OR_GREATER
    [Fact]
    public async Task ClassPrimaryConstructor_WithEmptyBraces()
    {
        await CreateProjectBuilder()
            .WithLanguageVersion(LanguageVersion.CSharp12)
            .WithSourceCode("""
                public class Foo(string Value1, string Value2) [|{|]}
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ClassPrimaryConstructor_CodeFix()
    {
        await CreateProjectBuilder()
            .WithLanguageVersion(LanguageVersion.CSharp12)
            .WithSourceCode("""
                public class Foo(string Value1, string Value2) [|{|]}
                """)
            .ShouldFixCodeWith("""
                public class Foo(string Value1, string Value2);
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StructPrimaryConstructor_CodeFix()
    {
        await CreateProjectBuilder()
            .WithLanguageVersion(LanguageVersion.CSharp12)
            .WithSourceCode("""
                public struct Foo(string Value1, string Value2) [|{|]}
                """)
            .ShouldFixCodeWith("""
                public struct Foo(string Value1, string Value2);
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ClassPrimaryConstructor_WithDocumentation()
    {
        await CreateProjectBuilder()
            .WithLanguageVersion(LanguageVersion.CSharp12)
            .WithSourceCode("""
                /// <summary>
                /// I show up when you hover my constructor invocation too!
                /// </summary>
                public sealed class Documented() [|{|]}
                """)
            .ShouldFixCodeWith("""
                /// <summary>
                /// I show up when you hover my constructor invocation too!
                /// </summary>
                public sealed class Documented();
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ClassWithoutPrimaryConstructor_WithDocumentation()
    {
        await CreateProjectBuilder()
            .WithLanguageVersion(LanguageVersion.CSharp12)
            .WithSourceCode("""
                /// <summary>
                /// I don't. :(
                /// </summary>
                public sealed class HalfDocumented [|{|]}
                """)
            .ShouldFixCodeWith("""
                /// <summary>
                /// I don't. :(
                /// </summary>
                public sealed class HalfDocumented;
                """)
            .ValidateAsync();
    }
#endif
}

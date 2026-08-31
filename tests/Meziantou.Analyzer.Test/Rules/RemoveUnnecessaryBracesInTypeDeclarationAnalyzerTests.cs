using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.RemoveUnnecessaryBracesInTypeDeclarationAnalyzer,
    Meziantou.Analyzer.Rules.RemoveUnnecessaryBracesInTypeDeclarationFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RemoveUnnecessaryBracesInTypeDeclarationAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task PositionalRecord_WithEmptyBraces()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp9;
        test.TestCode = """
            public record Foo(string Value1, string Value2) {|MA0206:{|}}
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PositionalRecord_CodeFix()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp9;
        test.TestCode = """
            public record Foo(string Value1, string Value2) {|MA0206:{|}}
            """;
        test.FixedCode = """
            public record Foo(string Value1, string Value2);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PositionalRecord_WithSemicolon()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp9;
        test.TestCode = """
            public record Foo(string Value1, string Value2);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PositionalRecord_WithMember()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp9;
        test.TestCode = """
            public record Foo(string Value1, string Value2)
            {
                public string Value3 { get; init; } = "";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PositionalRecord_WithComment()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp9;
        test.TestCode = """
            public record Foo(string Value1, string Value2)
            {
                // Keep this comment
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RecordWithoutParameterList()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp9;
        test.TestCode = """
            public record Foo {|MA0206:{|}}
            """;
        test.FixedCode = """
            public record Foo;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClassPrimaryConstructor_WithEmptyBraces()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            public class Foo(string Value1, string Value2) {|MA0206:{|}}
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClassPrimaryConstructor_CodeFix()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            public class Foo(string Value1, string Value2) {|MA0206:{|}}
            """;
        test.FixedCode = """
            public class Foo(string Value1, string Value2);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StructPrimaryConstructor_CodeFix()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            public struct Foo(string Value1, string Value2) {|MA0206:{|}}
            """;
        test.FixedCode = """
            public struct Foo(string Value1, string Value2);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClassPrimaryConstructor_WithDocumentation()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            /// <summary>
            /// I show up when you hover my constructor invocation too!
            /// </summary>
            public sealed class Documented() {|MA0206:{|}}
            """;
        test.FixedCode = """
            /// <summary>
            /// I show up when you hover my constructor invocation too!
            /// </summary>
            public sealed class Documented();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClassWithoutPrimaryConstructor_WithDocumentation()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            /// <summary>
            /// I don't. :(
            /// </summary>
            public sealed class HalfDocumented {|MA0206:{|}}
            """;
        test.FixedCode = """
            /// <summary>
            /// I don't. :(
            /// </summary>
            public sealed class HalfDocumented;
            """;

        return test.RunAsync();
    }
}

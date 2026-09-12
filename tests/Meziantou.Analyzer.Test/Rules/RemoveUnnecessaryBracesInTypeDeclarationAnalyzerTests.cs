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
        test.TestCode = """
            public record Foo(string Value1, string Value2) {|MA0206:{|}}
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PositionalRecord_CodeFix()
    {
        var test = CreateTest();
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
        test.TestCode = """
            public record Foo(string Value1, string Value2);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PositionalRecord_WithMember()
    {
        var test = CreateTest();
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
        test.TestCode = """
            public class Foo(string Value1, string Value2) {|MA0206:{|}}
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClassPrimaryConstructor_CodeFix()
    {
        var test = CreateTest();
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
        test.TestCode = """
            public struct Foo(string Value1, string Value2) {|MA0206:{|}}
            """;
        test.FixedCode = """
            public struct Foo(string Value1, string Value2);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RecordStructPrimaryConstructor_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            public record struct Foo(string Value1, string Value2) {|MA0206:{|}}
            """;
        test.FixedCode = """
            public record struct Foo(string Value1, string Value2);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReadOnlyRecordStructWithoutParameterList_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            public readonly record struct Foo {|MA0206:{|}}
            """;
        test.FixedCode = """
            public readonly record struct Foo;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RecordStruct_WithMember()
    {
        var test = CreateTest();
        test.TestCode = """
            public record struct Foo(string Value1)
            {
                public string Value2 { get; init; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            public interface IFoo {|MA0206:{|}}
            """;
        test.FixedCode = """
            public interface IFoo;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_WithBaseList_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            public interface IBase;
            public interface IFoo : IBase {|MA0206:{|}}
            """;
        test.FixedCode = """
            public interface IBase;
            public interface IFoo : IBase;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_WithMember()
    {
        var test = CreateTest();
        test.TestCode = """
            public interface IFoo
            {
                void Bar();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_CSharp11()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            public interface IFoo { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClassPrimaryConstructor_WithDocumentation()
    {
        var test = CreateTest();
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

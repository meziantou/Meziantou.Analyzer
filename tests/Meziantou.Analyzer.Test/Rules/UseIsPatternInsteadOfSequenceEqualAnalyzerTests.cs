using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseIsPatternInsteadOfSequenceEqualAnalyzer,
    Meziantou.Analyzer.Rules.UseIsPatternInsteadOfSequenceEqualFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseIsPatternInsteadOfSequenceEqualAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest { ReferenceAssemblies = ReferenceAssemblies.Net.Net70 };
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task EqualsOrdinal_CSharp10()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            _ = "foo".AsSpan().Equals("bar", StringComparison.Ordinal);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReadOnlySpanByte_SequenceEqual()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            _ = new byte[1].AsSpan().SequenceEqual(new byte[0].AsSpan());
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SpanByte_SequenceEqual()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            Span<byte> value = default;
            _ = value.SequenceEqual(new byte[0].AsSpan());
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReadOnlySpan_Equals()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            _ = "foo".AsSpan().Equals("value");
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EqualsOrdinal_NonConstant()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            string value = "test";
            _ = "foo".AsSpan().Equals(value, StringComparison.Ordinal);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SequenceEquals_NonConstant()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            string value = "test";
            _ = "foo".AsSpan().SequenceEqual(value);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SequenceEquals_Comparer()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            string value = "test";
            _ = "foo".AsSpan().SequenceEqual(value, default(System.Collections.Generic.IEqualityComparer<char>));
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReadOnlySpanChar_EqualsOrdinal()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            _ = {|MA0128:"foo".AsSpan().Equals("bar", StringComparison.Ordinal)|};
            """;
        test.FixedCode = """
            using System;
            _ = "foo".AsSpan() is "bar";
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReadOnlySpanChar_EqualsOrdinalIgnoreCase()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            _ = "foo".AsSpan().Equals("bar", StringComparison.OrdinalIgnoreCase);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReadOnlySpanChar_SequenceEqual()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            _ = {|MA0128:"foo".AsSpan().SequenceEqual("bar")|};
            """;
        test.FixedCode = """
            using System;
            _ = "foo".AsSpan() is "bar";
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SpanChar_SequenceEqual()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            Span<char> str = default;
            _ = {|MA0128:str.SequenceEqual("bar")|};
            """;
        test.FixedCode = """
            using System;
            Span<char> str = default;
            _ = str is "bar";
            """;

        return test.RunAsync();
    }
}

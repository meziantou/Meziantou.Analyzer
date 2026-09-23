using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.AddOverloadWithSpanOrMemoryAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AddOverloadWithSpanOrMemoryAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task EntryPoint_Main_ShouldNotTrigger()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            public class Program
            {
                public static void Main(string[] args) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EntryPoint_NonMainMethod_ShouldTrigger()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            public class Program
            {
                public static void Main(string[] args) { }
                public static void {|MA0109:DoWork|}(string[] data) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArrayWithoutSpanOverload_Params()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test
            {
                public void A(params string[] a)
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArrayWithoutSpanOverload_Out()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test
            {
                public void A(out byte[] a) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArrayWithSpanOverload_Params()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test
            {
                public void A(params string[] a) { }
                public void A(System.ReadOnlySpan<string> a) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArrayWithoutSpanOverload()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test
            {
                public void {|MA0109:A|}(string[] a)
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringArrayWithoutSpanOverload_Complex()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test
            {
                public void {|MA0109:A|}(string[] a, int b) { }
                public void A(System.ReadOnlySpan<string> a, string b) { } // Not same type for b
                public void A(System.ReadOnlySpan<string> a, int b, int c) { } // not same number of parameters
                public void A(System.ReadOnlySpan<string> a, System.ReadOnlySpan<int> b) { } // Not same type for b
                public void B(System.ReadOnlySpan<string> a, int b) { } // Not same method name
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("System.Span<string>")]
    [InlineData("System.ReadOnlySpan<string>")]
    [InlineData("System.Memory<string>")]
    [InlineData("System.ReadOnlyMemory<string>")]
    public Task StringArrayWithSpanOverload(string overloadType)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            public class Test
            {
                public void A(string[] a) { }
                public void A({{overloadType}} a) { }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("System.Span<char>")]
    [InlineData("System.ReadOnlySpan<char>")]
    [InlineData("System.Memory<char>")]
    [InlineData("System.ReadOnlyMemory<char>")]
    public Task StringArrayWithSpanOverload_String(string overloadType)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            public class Test
            {
                public void A(string a) { }
                public void A({{overloadType}} a) { }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("System.Span<string>")]
    [InlineData("System.ReadOnlySpan<string>")]
    [InlineData("System.Memory<string>")]
    [InlineData("System.ReadOnlyMemory<string>")]
    public Task SpanWithoutOverload(string overloadType)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            public class Test
            {
                public void A({{overloadType}} a) { }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("System.Span<T>")]
    [InlineData("System.ReadOnlySpan<T>")]
    [InlineData("System.Memory<T>")]
    [InlineData("System.ReadOnlyMemory<T>")]
    public Task GenericArrayWithSpanOverload(string overloadType)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            public class Sample
            {
                public void M<T>(T[] values) { }
                public void M<T>({{overloadType}} values) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericArrayWithSpanOverload_DifferentTypeParameterNames()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Sample
            {
                public void M<T>(T[] values, T value) { }
                public void M<TItem>(System.ReadOnlySpan<TItem> values, TItem value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericArrayWithNonGenericSpanOverload()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Sample
            {
                public void {|MA0109:M|}<T>(T[] values) { }
                public void M(System.ReadOnlySpan<int> values) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericArrayWithSpanOverloadOfAnotherTypeParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Sample
            {
                public void {|MA0109:M|}<T1, T2>(T1[] values, T2 value) { }
                public void M<T1, T2>(System.ReadOnlySpan<T2> values, T2 value) { }
            }
            """;

        return test.RunAsync();
    }
}

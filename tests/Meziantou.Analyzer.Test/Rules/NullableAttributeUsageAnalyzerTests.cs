using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.NullableAttributeUsageAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class NullableAttributeUsageAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task ParameterDoesNotExist()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [return: {|MA0068:System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("unknown")|}]
                public void A(string a) { }
            }

            namespace System.Diagnostics.CodeAnalysis
            {
                using System;

                [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
                public class NotNullIfNotNullAttribute : System.Attribute
                {
                    public NotNullIfNotNullAttribute (string parameterName) => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParameterExists()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("a")]
                public void A(string a) { }
            }

            namespace System.Diagnostics.CodeAnalysis
            {
                using System;

                [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
                public class NotNullIfNotNullAttribute : System.Attribute
                {
                    public NotNullIfNotNullAttribute (string parameterName) => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Parameter_ParameterDoesNotExist()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Test
            {
                public void A(string? a, [{|MA0068:NotNullIfNotNull("unknown")|}] out string? b) => b = a;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Parameter_ParameterExists()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Test
            {
                public void A(string? a, [NotNullIfNotNull(nameof(a))] out string? b) => b = a;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_ParameterDoesNotExist()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Test
            {
                [{|MA0068:NotNullIfNotNull("unknown")|}]
                public string? A => null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_ValueParameterOfTheSetter()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Test
            {
                [NotNullIfNotNull("value")]
                public string? A { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_ValueParameterOfAGetOnlyProperty()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Test
            {
                [{|MA0068:NotNullIfNotNull("value")|}]
                public string? A => null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Indexer_ParameterExists()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Test
            {
                [NotNullIfNotNull(nameof(key))]
                public string? this[string? key] => key;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Indexer_ParameterDoesNotExist()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Test
            {
                [{|MA0068:NotNullIfNotNull("unknown")|}]
                public string? this[string? key] => key;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IndexerParameter_ParameterDoesNotExist()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Test
            {
                public string? this[[{|MA0068:NotNullIfNotNull("unknown")|}] string? key] => key;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyAccessor_ParameterDoesNotExist()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Test
            {
                public string? A { [return: {|MA0068:NotNullIfNotNull("unknown")|}] get => null; }
            }
            """;

        return test.RunAsync();
    }

#if CSHARP14_OR_GREATER
    [Fact]
    public Task ExtensionBlock_ParameterExists_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            static class Extensions
            {
                extension(object? obj)
                {
                    [return: NotNullIfNotNull(nameof(obj))]
                    public object? DoSomething() => obj;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExtensionBlock_ParameterDoesNotExist_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            static class Extensions
            {
                extension(object? obj)
                {
                    [return: {|MA0068:NotNullIfNotNull("unknown")|}]
                    public object? DoSomething() => obj;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExtensionBlock_Property_ParameterExists_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            static class Extensions
            {
                extension(object? obj)
                {
                    [NotNullIfNotNull(nameof(obj))]
                    public object? Value => obj;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExtensionBlock_Property_ParameterDoesNotExist_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            static class Extensions
            {
                extension(object? obj)
                {
                    [{|MA0068:NotNullIfNotNull("unknown")|}]
                    public object? Value => obj;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExtensionBlock_Parameter_ParameterExists_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            static class Extensions
            {
                extension(object? obj)
                {
                    public void DoSomething([NotNullIfNotNull(nameof(obj))] out object? result) => result = obj;
                }
            }
            """;

        return test.RunAsync();
    }
#endif
}

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
                [return: [|System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("unknown")|]]
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
                    [return: [|NotNullIfNotNull("unknown")|]]
                    public object? DoSomething() => obj;
                }
            }
            """;

        return test.RunAsync();
    }
#endif
}

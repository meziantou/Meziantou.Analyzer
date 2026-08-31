using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UsePartialPropertyInsteadOfPartialMethodForGeneratedRegexAnalyzer,
    Meziantou.Analyzer.Rules.UsePartialPropertyInsteadOfPartialMethodForGeneratedRegexFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UsePartialPropertyInsteadOfPartialMethodForGeneratedRegexAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.UseFrameworkSourceGenerators = true;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.LanguageVersion = LanguageVersion.Preview;
        return test;
    }

    [Fact]
    public Task NoGeneratedRegexAttribute_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CSharp12_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            using System.Text.RegularExpressions;

            partial class Sample
            {
                [GeneratedRegex("pattern")]
                private static partial Regex SampleRegex();
            }
            """;

        return test.RunAsync();
    }

#if CSHARP13_OR_GREATER
    [Fact]
    public Task GeneratedRegexPartialMethod_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.RegularExpressions;

            partial class Sample
            {
                [GeneratedRegex("pattern")]
                private static partial Regex [|SampleRegex|]();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GeneratedRegexPartialMethod_WithOptions_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.RegularExpressions;

            partial class Sample
            {
                [GeneratedRegex("pattern", RegexOptions.CultureInvariant)]
                private static partial Regex [|SampleRegex|]();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_ConvertsMethodToProperty()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.RegularExpressions;

            partial class Sample
            {
                [GeneratedRegex("pattern")]
                private static partial Regex [|SampleRegex|]();
            }
            """;
        test.FixedCode = """
            using System.Text.RegularExpressions;

            partial class Sample
            {
                [GeneratedRegex("pattern")]
                private static partial Regex SampleRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_ConvertsMethodToProperty_WithTimeout()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.RegularExpressions;
            using System.Threading;

            partial class Sample
            {
                [GeneratedRegex(@"sample.*", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: Timeout.Infinite)]
                private static partial Regex [|SampleRegex|]();
            }
            """;
        test.FixedCode = """
            using System.Text.RegularExpressions;
            using System.Threading;

            partial class Sample
            {
                [GeneratedRegex(@"sample.*", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: Timeout.Infinite)]
                private static partial Regex SampleRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_ReplacesInvocationsWithPropertyAccess()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.RegularExpressions;

            partial class Sample
            {
                [GeneratedRegex("pattern")]
                private static partial Regex [|SampleRegex|]();

                void M()
                {
                    _ = SampleRegex().IsMatch("value");
                }
            }
            """;
        test.FixedCode = """
            using System.Text.RegularExpressions;

            partial class Sample
            {
                [GeneratedRegex("pattern")]
                private static partial Regex SampleRegex { get; }

                void M()
                {
                    _ = SampleRegex.IsMatch("value");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_ReplacesMultipleInvocationsWithPropertyAccess()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.RegularExpressions;

            partial class Sample
            {
                [GeneratedRegex("pattern")]
                private static partial Regex [|SampleRegex|]();

                void M()
                {
                    _ = SampleRegex().IsMatch("value");
                    _ = SampleRegex().Match("value");
                }
            }
            """;
        test.FixedCode = """
            using System.Text.RegularExpressions;

            partial class Sample
            {
                [GeneratedRegex("pattern")]
                private static partial Regex SampleRegex { get; }

                void M()
                {
                    _ = SampleRegex.IsMatch("value");
                    _ = SampleRegex.Match("value");
                }
            }
            """;

        return test.RunAsync();
    }
#endif
}

using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UsePartialPropertyInsteadOfPartialMethodForGeneratedRegexAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.Net9_0)
            .WithFrameworkSourceGenerators()
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)
            .WithAnalyzer<UsePartialPropertyInsteadOfPartialMethodForGeneratedRegexAnalyzer>()
            .WithCodeFixProvider<UsePartialPropertyInsteadOfPartialMethodForGeneratedRegexFixer>()
            .WithNoFixCompilation();
    }

    [Fact]
    public async Task CSharp12_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
            .WithSourceCode("""
                using System.Text.RegularExpressions;

                partial class Sample
                {
                    [GeneratedRegex("pattern")]
                    private static partial Regex SampleRegex();
                }
                """)
            .ValidateAsync();
    }

#if CSHARP13_OR_GREATER
    [Fact]
    public async Task GeneratedRegexPartialMethod_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Text.RegularExpressions;

                partial class Sample
                {
                    [GeneratedRegex("pattern")]
                    private static partial Regex [|SampleRegex|]();
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task GeneratedRegexPartialMethod_WithOptions_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Text.RegularExpressions;

                partial class Sample
                {
                    [GeneratedRegex("pattern", RegexOptions.CultureInvariant)]
                    private static partial Regex [|SampleRegex|]();
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_ConvertsMethodToProperty()
    {
        const string sourceCode = """
            using System.Text.RegularExpressions;

            partial class Sample
            {
                [GeneratedRegex("pattern")]
                private static partial Regex [|SampleRegex|]();
            }
            """;

        const string expectedFix = """
            using System.Text.RegularExpressions;

            partial class Sample
            {
                [GeneratedRegex("pattern")]
                private static partial Regex SampleRegex { get; }
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(sourceCode)
            .ShouldFixCodeWith(expectedFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_ConvertsMethodToProperty_WithTimeout()
    {
        const string sourceCode = """
            using System.Text.RegularExpressions;
            using System.Threading;

            partial class Sample
            {
                [GeneratedRegex(@"sample.*", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: Timeout.Infinite)]
                private static partial Regex [|SampleRegex|]();
            }
            """;

        const string expectedFix = """
            using System.Text.RegularExpressions;
            using System.Threading;

            partial class Sample
            {
                [GeneratedRegex(@"sample.*", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: Timeout.Infinite)]
                private static partial Regex SampleRegex { get; }
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(sourceCode)
            .ShouldFixCodeWith(expectedFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_ReplacesInvocationsWithPropertyAccess()
    {
        const string sourceCode = """
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

        const string expectedFix = """
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

        await CreateProjectBuilder()
            .WithSourceCode(sourceCode)
            .ShouldFixCodeWith(expectedFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_ReplacesMultipleInvocationsWithPropertyAccess()
    {
        const string sourceCode = """
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

        const string expectedFix = """
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

        await CreateProjectBuilder()
            .WithSourceCode(sourceCode)
            .ShouldFixCodeWith(expectedFix)
            .ValidateAsync();
    }
#endif
}

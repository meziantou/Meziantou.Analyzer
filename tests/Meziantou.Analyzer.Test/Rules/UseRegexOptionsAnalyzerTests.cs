using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.RegexMethodUsageAnalyzer,
    Meziantou.Analyzer.Rules.UseRegexExplicitCaptureOptionsFixer>;
using GeneratedRegexCodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.GeneratedRegexAttributeUsageAnalyzer,
    Meziantou.Analyzer.Rules.UseRegexExplicitCaptureOptionsFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseRegexOptionsAnalyzerTests
{
    // The Regex source generator does not run, so the partial members the tests declare are given a dummy
    // implementation part, which the generator would otherwise provide.
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.AdditionalAnalyzers.Add(new Meziantou.Analyzer.Rules.GeneratedRegexAttributeUsageAnalyzer());

        // Both analyzers derive from RegexUsageAnalyzerBase, so they report the same descriptors
        test.MarkupOptions = MarkupOptions.UseFirstDescriptor;
        return test;
    }

    private static string MarkOptions(string options, bool isValid) => isValid ? options : "{|MA0023:" + options + "|}";

    [Theory]
    [InlineData("([a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", false)]
    [InlineData("([a-z]+)", "RegexOptions.None", false)]
    [InlineData("([a-z]+)", "RegexOptions.ExplicitCapture", true)]
    [InlineData("(?<test>[a-z]+)", "RegexOptions.None", true)]
    public Task IsMatch_RegexOptions(string regex, string options, bool isValid)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text.RegularExpressions;
            class TestClass
            {
                void Test()
                {
                    Regex.IsMatch("test", "{{regex}}", {{MarkOptions(options, isValid)}}, default);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsMatch_RegexOptions_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.RegularExpressions;
            class TestClass
            {
                void Test()
                {
                    Regex.IsMatch("test", "([a-z]+)", {|MA0023:RegexOptions.None|}, default);
                }
            }
            """;
        test.FixedCode = """
            using System.Text.RegularExpressions;
            class TestClass
            {
                void Test()
                {
                    Regex.IsMatch("test", "([a-z]+)", RegexOptions.None | RegexOptions.ExplicitCapture, default);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("([a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", false)]
    [InlineData("(?<test>[a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", true)]
    [InlineData("[a-z]+", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", true)]
    [InlineData("[a-z]+", "RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase", true)]
    [InlineData("[a-z]+", "RegexOptions.ECMAScript", true)]
    [InlineData("([a-z]+)", "RegexOptions.ECMAScript", true)]
    public Task Ctor_RegexOptions(string regex, string options, bool isValid)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text.RegularExpressions;
            class TestClass
            {
                void Test()
                {
                    new Regex("{{regex}}", {{MarkOptions(options, isValid)}}, default);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("(?<test>[a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase")]
    [InlineData("[a-z]+", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase")]
    [InlineData("[a-z]+", "RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase")]
    [InlineData("[a-z]+", "RegexOptions.ECMAScript")]
    [InlineData("([a-z]+)", "RegexOptions.ECMAScript")]
    public Task GeneratedRegex_RegexOptions_Valid(string regex, string options)
    {
        var test = CreateTest();
        test.UseFrameworkSourceGenerators = true;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = $$"""
            using System.Text.RegularExpressions;
            partial class TestClass
            {
                [GeneratedRegex("{{regex}}", {{options}}, -1)]
                private static partial Regex Test();
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("([a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase")]
    public Task GeneratedRegex_RegexOptions_Invalid(string regex, string options)
    {
        var test = CreateTest();
        test.UseFrameworkSourceGenerators = true;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = $$"""
            using System.Text.RegularExpressions;
            partial class TestClass
            {
                [{|MA0023:GeneratedRegex("{{regex}}", {{options}}, -1)|}]
                private static partial Regex Test();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GeneratedRegex_RegexOptions_Invalid_CodeFix()
    {
        var test = new GeneratedRegexCodeFixTest();
        test.UseFrameworkSourceGenerators = true;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = """
            using System.Text.RegularExpressions;
            partial class TestClass
            {
                [{|MA0023:GeneratedRegex("([a-z]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, -1)|}]
                private static partial Regex Test();
            }
            """;
        test.FixedCode = """
            using System.Text.RegularExpressions;
            partial class TestClass
            {
                [GeneratedRegex("([a-z]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, -1)]
                private static partial Regex Test();
            }
            """;

        return test.RunAsync();
    }

#if CSHARP13_OR_GREATER
    [Fact]
    public Task GeneratedRegexProperty_RegexOptions_Valid()
    {
        var test = CreateTest();
        test.UseFrameworkSourceGenerators = true;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System.Text.RegularExpressions;

            partial class TestClass
            {
                [GeneratedRegex("(?<test>[a-z]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, -1)]
                private static partial Regex Test { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GeneratedRegexProperty_RegexOptions_Invalid()
    {
        var test = CreateTest();
        test.UseFrameworkSourceGenerators = true;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System.Text.RegularExpressions;

            partial class TestClass
            {
                [{|MA0023:GeneratedRegex("([a-z]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, -1)|}]
                private static partial Regex Test { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GeneratedRegexProperty_RegexOptions_Invalid_CodeFix()
    {
        var test = new GeneratedRegexCodeFixTest();
        test.UseFrameworkSourceGenerators = true;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System.Text.RegularExpressions;

            partial class TestClass
            {
                [{|MA0023:GeneratedRegex("([a-z]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, -1)|}]
                private static partial Regex Test { get; }
            }
            """;
        test.FixedCode = """
            using System.Text.RegularExpressions;

            partial class TestClass
            {
                [GeneratedRegex("([a-z]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, -1)]
                private static partial Regex Test { get; }
            }
            """;

        return test.RunAsync();
    }
#endif
}

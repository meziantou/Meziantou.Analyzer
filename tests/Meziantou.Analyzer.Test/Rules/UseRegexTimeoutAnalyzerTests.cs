using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.RegexMethodUsageAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseRegexTimeoutAnalyzerTests
{
    // The Regex source generator does not run, so the partial members the tests declare are given a dummy
    // implementation part, which the generator would otherwise provide.
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.AdditionalAnalyzers.Add(new Meziantou.Analyzer.Rules.GeneratedRegexAttributeUsageAnalyzer());

        // Both analyzers derive from RegexUsageAnalyzerBase, so they report the same descriptors
        test.MarkupOptions = MarkupOptions.UseFirstDescriptor;
        return test;
    }

    [Fact]
    public Task IsMatch_MissingTimeout_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.RegularExpressions;
            class TestClass
            {
                void Test()
                {
                    {|MA0009:Regex.IsMatch("test", "[a-z]+")|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsMatch_WithTimeout_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.RegularExpressions;
            class TestClass
            {
                void Test()
                {
                    Regex.IsMatch("test", "[a-z]+", RegexOptions.ExplicitCapture, System.TimeSpan.FromSeconds(1));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IsMatch_NonBacktracking_WithoutTimeout_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.RegularExpressions;
            class TestClass
            {
                void Test()
                {
                    Regex.IsMatch("test", "[a-z]+", RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Ctor_MissingTimeout_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.RegularExpressions;
            class TestClass
            {
                void Test()
                {
                    {|MA0009:new Regex("[a-z]+")|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Ctor_WithTimeout_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.RegularExpressions;
            class TestClass
            {
                void Test()
                {
                    new Regex("[a-z]+", RegexOptions.ExplicitCapture, System.TimeSpan.FromSeconds(1));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Ctor_WithoutTimeout_NonBacktracking_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.RegularExpressions;
            class TestClass
            {
                void Test()
                {
                    new Regex("[a-z]+", RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonRegexCtor_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    new System.Exception("");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GeneratedRegex_WithoutTimeout()
    {
        var test = CreateTest();
        test.UseFrameworkSourceGenerators = true;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = """
            using System.Text.RegularExpressions;
            partial class TestClass
            {
                [{|MA0009:GeneratedRegex("pattern", RegexOptions.None)|}]
                private static partial Regex Test();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GeneratedRegex_WithTimeout()
    {
        var test = CreateTest();
        test.UseFrameworkSourceGenerators = true;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = """
            using System.Text.RegularExpressions;
            partial class TestClass
            {
                [GeneratedRegex("pattern", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
                private static partial Regex Test();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GeneratedRegex_WithoutTimeout_NonBacktracking()
    {
        var test = CreateTest();
        test.UseFrameworkSourceGenerators = true;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = """
            using System.Text.RegularExpressions;
            partial class TestClass
            {
                [GeneratedRegex("pattern", RegexOptions.NonBacktracking)]
                private static partial Regex Test();
            }
            """;

        // The generator cannot produce a complete implementation for a NonBacktracking regex
        test.ExpectedDiagnostics.Add(new DiagnosticResult("SYSLIB1044", DiagnosticSeverity.Info).WithSpan(4, 5, 5, 41));

        return test.RunAsync();
    }

#if CSHARP13_OR_GREATER
    [Fact]
    public Task GeneratedRegexProperty_WithoutTimeout()
    {
        var test = CreateTest();
        test.UseFrameworkSourceGenerators = true;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System.Text.RegularExpressions;

            partial class TestClass
            {
                [{|MA0009:GeneratedRegex("pattern", RegexOptions.None)|}]
                private static partial Regex Test { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GeneratedRegexProperty_WithTimeout()
    {
        var test = CreateTest();
        test.UseFrameworkSourceGenerators = true;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System.Text.RegularExpressions;

            partial class TestClass
            {
                [GeneratedRegex("pattern", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
                private static partial Regex Test { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GeneratedRegexProperty_WithoutTimeout_NonBacktracking()
    {
        var test = CreateTest();
        test.UseFrameworkSourceGenerators = true;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System.Text.RegularExpressions;

            partial class TestClass
            {
                [GeneratedRegex("pattern", RegexOptions.NonBacktracking)]
                private static partial Regex Test { get; }
            }
            """;

        // The generator cannot produce a complete implementation for a NonBacktracking regex
        test.ExpectedDiagnostics.Add(new DiagnosticResult("SYSLIB1044", DiagnosticSeverity.Info).WithSpan(5, 5, 6, 47));

        return test.RunAsync();
    }
#endif
}

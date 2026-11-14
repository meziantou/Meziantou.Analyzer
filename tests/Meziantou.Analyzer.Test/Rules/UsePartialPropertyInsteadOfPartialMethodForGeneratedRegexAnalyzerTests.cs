using Microsoft.CodeAnalysis;
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

    [Fact]
    public Task Net80_AttributeCannotBeAppliedToProperties_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.LanguageVersion = LanguageVersion.CSharp13;
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

    [Fact]
    public Task GeneratedRegexPartialMethod_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.RegularExpressions;

            partial class Sample
            {
                [GeneratedRegex("pattern")]
                private static partial Regex {|MA0190:SampleRegex|}();
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
                private static partial Regex {|MA0190:SampleRegex|}();
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
                private static partial Regex {|MA0190:SampleRegex|}();
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
                private static partial Regex {|MA0190:SampleRegex|}();
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
                private static partial Regex {|MA0190:SampleRegex|}();

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
                private static partial Regex {|MA0190:SampleRegex|}();

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

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1811")]
    public Task CodeFix_InvocationUsedAsStatement()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Sample
            {
                void A()
                {
                    SampleRegex();
                    Sample.SampleRegex();
                    for (SampleRegex(); ; SampleRegex())
                    {
                    }
                }

                void B() => SampleRegex();

                void C()
                {
                    Action action = () => SampleRegex();
                    Func<Regex> func = () => SampleRegex();
                }

                [GeneratedRegex("[a-z]+")]
                private static partial Regex {|MA0190:SampleRegex|}();
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Sample
            {
                void A()
                {
                    _ = SampleRegex;
                    _ = Sample.SampleRegex;
                    for (_ = SampleRegex; ; _ = SampleRegex)
                    {
                    }
                }

                void B() => _ = SampleRegex;

                void C()
                {
                    Action action = () => _ = SampleRegex;
                    Func<Regex> func = () => SampleRegex;
                }

                [GeneratedRegex("[a-z]+")]
                private static partial Regex SampleRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_MethodGroup()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Sample
            {
                void A()
                {
                    Func<Regex> func1 = SampleRegex;
                    Func<Regex> func2 = Sample.SampleRegex;
                    Use(SampleRegex);
                    _ = nameof(SampleRegex);
                }

                static void Use(Func<Regex> func) { }

                [GeneratedRegex("[a-z]+")]
                private static partial Regex {|MA0190:SampleRegex|}();
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Sample
            {
                void A()
                {
                    Func<Regex> func1 = () => SampleRegex;
                    Func<Regex> func2 = () => Sample.SampleRegex;
                    Use(() => SampleRegex);
                    _ = nameof(SampleRegex);
                }

                static void Use(Func<Regex> func) { }

                [GeneratedRegex("[a-z]+")]
                private static partial Regex SampleRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CodeFix_Cref()
    {
        var test = CreateTest();
        test.TestState.DocumentationMode = DocumentationMode.Diagnose;
        test.TestCode = """
            using System.Text.RegularExpressions;

            /// <summary><see cref="SampleRegex()"/> <see cref="Sample.SampleRegex()"/> <see cref="SampleRegex"/></summary>
            public partial class Sample
            {
                [GeneratedRegex("[a-z]+")]
                private static partial Regex {|MA0190:SampleRegex|}();
            }
            """;
        test.FixedCode = """
            using System.Text.RegularExpressions;

            /// <summary><see cref="SampleRegex"/> <see cref="Sample.SampleRegex"/> <see cref="SampleRegex"/></summary>
            public partial class Sample
            {
                [GeneratedRegex("[a-z]+")]
                private static partial Regex SampleRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/1679")]
    public Task InvalidCode_ReferenceBeforeDeclaration()
    {
        var test = CreateTest();
        // The code does not compile, so the compiler diagnostics cannot be verified
        test.CompilerDiagnostics = CompilerDiagnostics.None;
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Text.RegularExpressions;

            _ = EmailRegex(

            partial class Program
            {
                [GeneratedRegex("a")]
                private static partial Regex {|MA0190:EmailRegex|}(
            """;
        test.FixedCode = """
            using System.Text.RegularExpressions;

            _ = EmailRegex
            partial class Program
            {
                [GeneratedRegex("a")]
                private static partial Regex EmailRegex { get; }
            """;

        return test.RunAsync();
    }
}

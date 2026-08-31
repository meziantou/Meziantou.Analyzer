using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseRegexSourceGeneratorAnalyzer,
    Meziantou.Analyzer.Rules.UseRegexSourceGeneratorFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public class UseRegexSourceGeneratorAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.UseFrameworkSourceGenerators = true;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.LanguageVersion = LanguageVersion.Preview;
        return test;
    }

    [Fact]
    public Task NewRegex_Options_Timeout()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                Regex a = {|MA0110:new Regex("testpattern", RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                Regex a = MyRegex();

                [GeneratedRegex("testpattern", RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NewRegex_Options()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                Regex a = {|MA0110:new Regex("testpattern", RegexOptions.ExplicitCapture)|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                Regex a = MyRegex();

                [GeneratedRegex("testpattern", RegexOptions.ExplicitCapture)]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NewRegex()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                Regex a = {|MA0110:new Regex("testpattern")|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                Regex a = MyRegex();

                [GeneratedRegex("testpattern")]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RegexIsMatch_Options_Timeout()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                bool a = {|MA0110:Regex.IsMatch("test", "testpattern", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1))|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                bool a = MyRegex().IsMatch("test");

                [GeneratedRegex("testpattern", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RegexIsMatch_Options()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                bool a = {|MA0110:Regex.IsMatch("test", "testpattern", RegexOptions.ExplicitCapture)|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                bool a = MyRegex().IsMatch("test");

                [GeneratedRegex("testpattern", RegexOptions.ExplicitCapture)]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RegexIsMatch()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                bool a = {|MA0110:Regex.IsMatch("test", "testpattern")|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                bool a = MyRegex().IsMatch("test");

                [GeneratedRegex("testpattern")]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RegexReplace_Options_Timeout()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                string a = {|MA0110:Regex.Replace("test", "testpattern", "newValue", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1))|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                string a = MyRegex().Replace("test", "newValue");

                [GeneratedRegex("testpattern", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RegexReplace_Options()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                string a = {|MA0110:Regex.Replace("test", "testpattern", "newValue", RegexOptions.ExplicitCapture)|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                string a = MyRegex().Replace("test", "newValue");

                [GeneratedRegex("testpattern", RegexOptions.ExplicitCapture)]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RegexReplace()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                string a = {|MA0110:Regex.Replace("test", "testpattern", "newValue")|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                string a = MyRegex().Replace("test", "newValue");

                [GeneratedRegex("testpattern")]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RegexReplace_MatchEvaluator()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                string a = {|MA0110:Regex.Replace("test", "testpattern", evaluator: match => "")|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                string a = MyRegex().Replace("test", evaluator: match => "");

                [GeneratedRegex("testpattern")]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("TimeSpan.Zero")]
    [InlineData("TimeSpan.FromMilliseconds(-2)")]
    public Task Timeout_NotSupportedByTheGenerator_NoDiagnostic(string timeout)
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;

        // GeneratedRegex only accepts an infinite or strictly positive match timeout
        test.TestCode = $$"""
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                Regex a = new Regex("testpattern", RegexOptions.None, {{timeout}});
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("TimeSpan.FromMilliseconds(10)", "10")]
    [InlineData("TimeSpan.FromSeconds(10.5)", "10500")]
    [InlineData("TimeSpan.FromMinutes(1)", "60000")]
    [InlineData("TimeSpan.FromHours(1)", "3600000")]
    [InlineData("TimeSpan.FromDays(1)", "86400000")]
    [InlineData("new TimeSpan(10000)", "1")]
    [InlineData("new TimeSpan(1, 2, 3)", "3723000")]
    [InlineData("new TimeSpan(1, 2, 3, 4)", "93784000")]
    [InlineData("new TimeSpan(1, 2, 3, 4, 5)", "93784005")]
    public Task Timeout(string timeout, string milliseconds)
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = $$"""
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                Regex a = {|MA0110:new Regex("testpattern", RegexOptions.None, {{timeout}})|};
            }
            """;
        test.FixedCode = $$"""
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                Regex a = MyRegex();

                [GeneratedRegex("testpattern", RegexOptions.None, matchTimeoutMilliseconds: {{milliseconds}})]
                private static partial Regex MyRegex();
            }
            """;


        return test.RunAsync();
    }

    [Theory]
    [InlineData("System.Threading.Timeout.InfiniteTimeSpan")]
    [InlineData("Regex.InfiniteMatchTimeout")]
    public Task New_Timeout_Infinite(string timeout)
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = $$"""
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                Regex a = {|MA0110:new Regex("testpattern", RegexOptions.None, {{timeout}})|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                Regex a = MyRegex();

                [GeneratedRegex("testpattern", RegexOptions.None, matchTimeoutMilliseconds: -1)]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("System.Threading.Timeout.InfiniteTimeSpan")]
    [InlineData("Regex.InfiniteMatchTimeout")]
    public Task Static_Timeout_Infinite(string timeout)
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = $$"""
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                bool a = {|MA0110:Regex.IsMatch("input", "testpattern", RegexOptions.None, {{timeout}})|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                bool a = MyRegex().IsMatch("input");

                [GeneratedRegex("testpattern", RegexOptions.None, matchTimeoutMilliseconds: -1)]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenerateUniqueMethodName()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                bool a = {|MA0110:Regex.IsMatch("input", "testpattern")|};

                private static Regex MyRegex() => throw null;
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                bool a = MyRegex_().IsMatch("input");

                private static Regex MyRegex() => throw null;
                [GeneratedRegex("testpattern")]
                private static partial Regex MyRegex_();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonConstantPattern()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                void A(string pattern) => Regex.IsMatch("input", pattern);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NestedTypeShouldAddPartialToAllAncestorTypes()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Sample
            {
                private partial class Inner1
                {
                    class Inner
                    {
                        bool a = {|MA0110:Regex.IsMatch("input", "testpattern")|};
                    }
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Sample
            {
                private partial class Inner1
                {
                    partial class Inner
                    {
                        bool a = MyRegex().IsMatch("input");

                        [GeneratedRegex("testpattern")]
                        private static partial Regex MyRegex();
                    }
                }
            }
            """;

        return test.RunAsync();
    }

#if CSHARP14_OR_GREATER
    [Fact]
    public Task NewRegex_PartialProperty()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                Regex a = {|MA0110:new Regex("testpattern")|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                [GeneratedRegex("testpattern")]
                private static partial Regex MyRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NewRegex_Options_PartialProperty()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                Regex a = {|MA0110:new Regex("testpattern", RegexOptions.ExplicitCapture)|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                [GeneratedRegex("testpattern", RegexOptions.ExplicitCapture)]
                private static partial Regex MyRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NewRegex_Options_Timeout_PartialProperty()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                Regex a = {|MA0110:new Regex("testpattern", RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                [GeneratedRegex("testpattern", RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
                private static partial Regex MyRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RegexIsMatch_PartialProperty()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                bool a = {|MA0110:Regex.IsMatch("test", "testpattern")|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                bool a = MyRegex.IsMatch("test");

                [GeneratedRegex("testpattern")]
                private static partial Regex MyRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RegexIsMatch_Options_PartialProperty()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                bool a = {|MA0110:Regex.IsMatch("test", "testpattern", RegexOptions.ExplicitCapture)|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                bool a = MyRegex.IsMatch("test");

                [GeneratedRegex("testpattern", RegexOptions.ExplicitCapture)]
                private static partial Regex MyRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RegexReplace_PartialProperty()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                string a = {|MA0110:Regex.Replace("test", "testpattern", "newValue")|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                string a = MyRegex.Replace("test", "newValue");

                [GeneratedRegex("testpattern")]
                private static partial Regex MyRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NestedTypeShouldAddPartialToAllAncestorTypes_PartialProperty()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Sample
            {
                private partial class Inner1
                {
                    class Inner
                    {
                        bool a = {|MA0110:Regex.IsMatch("input", "testpattern")|};
                    }
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Sample
            {
                private partial class Inner1
                {
                    partial class Inner
                    {
                        bool a = MyRegex.IsMatch("input");

                        [GeneratedRegex("testpattern")]
                        private static partial Regex MyRegex { get; }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

#endif
    [Fact]
    public Task Field_SuggestFieldName()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Sample
            {
                private static readonly Regex SampleRegex = {|MA0110:new Regex("pattern")|};

                void M()
                {
                    _ = SampleRegex.IsMatch("value");
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Sample
            {
                private static readonly Regex SampleRegex = SampleRegex_();

                void M()
                {
                    _ = SampleRegex.IsMatch("value");
                }

                [GeneratedRegex("pattern")]
                private static partial Regex SampleRegex_();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_SuggestFieldNameWithoutRegexSuffix()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Sample
            {
                private static readonly Regex EmailPattern = {|MA0110:new Regex("pattern")|};

                void M()
                {
                    _ = EmailPattern.IsMatch("value");
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Sample
            {
                private static readonly Regex EmailPattern = EmailPattern_();

                void M()
                {
                    _ = EmailPattern.IsMatch("value");
                }

                [GeneratedRegex("pattern")]
                private static partial Regex EmailPattern_();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Variable_SuggestPascalCaseName()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Foo
            {
                void A()
                {
                    Regex sampleRegex = {|MA0110:new Regex("pattern")|};
                    _ = sampleRegex.IsMatch("value");
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Foo
            {
                void A()
                {
                    Regex sampleRegex = SampleRegex();
                    _ = sampleRegex.IsMatch("value");
                }

                [GeneratedRegex("pattern")]
                private static partial Regex SampleRegex();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Variable_AlreadyPascalCase()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Foo
            {
                void A()
                {
                    Regex EmailRegex = {|MA0110:new Regex("pattern")|};
                    _ = EmailRegex.IsMatch("value");
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Foo
            {
                void A()
                {
                    Regex EmailRegex = EmailRegex_();
                    _ = EmailRegex.IsMatch("value");
                }

                [GeneratedRegex("pattern")]
                private static partial Regex EmailRegex_();
            }
            """;
        return test.RunAsync();
    }

    [Fact]
    public Task StaticMethod_UseDefaultName()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test
            {
                bool a = {|MA0110:Regex.IsMatch("test", "testpattern")|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test
            {
                bool a = MyRegex().IsMatch("test");

                [GeneratedRegex("testpattern")]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }

#if CSHARP14_OR_GREATER
    [Fact]
    public Task Field_RemoveAndReplaceWithProperty_PartialProperty()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Sample
            {
                private static readonly Regex SampleRegex = {|MA0110:new Regex("pattern")|};

                void M()
                {
                    _ = SampleRegex.IsMatch("value");
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Sample
            {

                void M()
                {
                    _ = SampleRegex.IsMatch("value");
                }

                [GeneratedRegex("pattern")]
                private static partial Regex SampleRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Variable_RemoveAndReplaceWithProperty_PartialProperty()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Foo
            {
                void A()
                {
                    Regex sampleRegex = {|MA0110:new Regex("pattern")|};
                    _ = sampleRegex.IsMatch("value");
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Foo
            {
                void A()
                {
                    _ = SampleRegex.IsMatch("value");
                }

                [GeneratedRegex("pattern")]
                private static partial Regex SampleRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyInitializer_RemoveAndReplaceWithProperty_PartialMethod()
    {
        var test = new CodeFixTest();
        test.UseFrameworkSourceGenerators = true;
        test.CodeActionIndex = 1;
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Sample
            {
                public Regex SampleRegex { get; } = {|MA0110:new Regex("pattern")|};

                void M()
                {
                    _ = SampleRegex.IsMatch("value");
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Sample
            {
                public Regex SampleRegex { get; } = SampleRegex_();

                void M()
                {
                    _ = SampleRegex.IsMatch("value");
                }

                [GeneratedRegex("pattern")]
                private static partial Regex SampleRegex_();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyInitializer_NoReferences_RemoveAndReplaceWithProperty_PartialMethod()
    {
        var test = new CodeFixTest();
        test.UseFrameworkSourceGenerators = true;
        test.CodeActionIndex = 1;
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Sample
            {
                public Regex EmailRegex { get; } = {|MA0110:new Regex("pattern")|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Sample
            {
                public Regex EmailRegex { get; } = EmailRegex_();

                [GeneratedRegex("pattern")]
                private static partial Regex EmailRegex_();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyInitializer_WithOptions_RemoveAndReplaceWithProperty_PartialMethod()
    {
        var test = new CodeFixTest();
        test.UseFrameworkSourceGenerators = true;
        test.CodeActionIndex = 1;
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Sample
            {
                public Regex Pattern { get; } = {|MA0110:new Regex("pattern", RegexOptions.IgnoreCase)|};

                void M()
                {
                    _ = Pattern.IsMatch("value");
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Sample
            {
                public Regex Pattern { get; } = Pattern_();

                void M()
                {
                    _ = Pattern.IsMatch("value");
                }

                [GeneratedRegex("pattern", RegexOptions.IgnoreCase)]
                private static partial Regex Pattern_();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyInitializer_PartialMethod()
    {
        var test = new CodeFixTest();
        test.UseFrameworkSourceGenerators = true;
        test.CodeActionIndex = 1;
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Sample
            {
                public Regex Pattern { get; } = {|MA0110:new Regex("pattern")|};

                void M()
                {
                    _ = Pattern.IsMatch("value");
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Sample
            {
                public Regex Pattern { get; } = Pattern_();

                void M()
                {
                    _ = Pattern.IsMatch("value");
                }

                [GeneratedRegex("pattern")]
                private static partial Regex Pattern_();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_MultipleReferences_RemoveAndReplaceWithProperty_PartialProperty()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Sample
            {
                private static readonly Regex EmailPattern = {|MA0110:new Regex("pattern")|};

                void M1()
                {
                    _ = EmailPattern.IsMatch("value1");
                }

                void M2()
                {
                    _ = EmailPattern.IsMatch("value2");
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Sample
            {

                void M1()
                {
                    _ = EmailPattern.IsMatch("value1");
                }

                void M2()
                {
                    _ = EmailPattern.IsMatch("value2");
                }

                [GeneratedRegex("pattern")]
                private static partial Regex EmailPattern { get; }
            }
            """;

        return test.RunAsync();
    }

#endif
    [Fact]
    public Task TopLevelStatement_NewRegex_PartialMethod()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Text.RegularExpressions;

            var emailRegex = {|MA0110:new Regex("pattern")|};
            emailRegex.Match("value");
            """;
        test.FixedCode = """
            using System.Text.RegularExpressions;

            var emailRegex = EmailRegex();
            emailRegex.Match("value");

            partial class Program
            {
                [GeneratedRegex("pattern")]
                private static partial Regex EmailRegex();
            }
            """;

        return test.RunAsync();
    }

#if CSHARP14_OR_GREATER
    [Fact]
    public Task TopLevelStatement_NewRegex_PartialProperty()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Text.RegularExpressions;

            var emailRegex = {|MA0110:new Regex("pattern")|};
            emailRegex.Match("value");
            """;
        test.FixedCode = """
            using System.Text.RegularExpressions;
            EmailRegex.Match("value");

            partial class Program
            {
                [GeneratedRegex("pattern")]
                private static partial Regex EmailRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TopLevelStatement_StaticMethod_PartialProperty()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Text.RegularExpressions;

            var result = {|MA0110:Regex.IsMatch("test", "pattern")|};
            """;
        test.FixedCode = """
            using System.Text.RegularExpressions;

            var result = MyRegex.IsMatch("test");

            partial class Program
            {
                [GeneratedRegex("pattern")]
                private static partial Regex MyRegex { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TopLevelStatement_WithExistingProgramClass_PartialProperty()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Text.RegularExpressions;

            var result = {|MA0110:Regex.IsMatch("test", "pattern")|};

            partial class Program
            {
                private static void Helper() { }
            }
            """;
        test.FixedCode = """
            using System.Text.RegularExpressions;

            var result = MyRegex.IsMatch("test");

            partial class Program
            {
                private static void Helper() { }

                [GeneratedRegex("pattern")]
                private static partial Regex MyRegex { get; }
            }
            """;

        return test.RunAsync();
    }

#endif
    [Fact]
    public Task TopLevelStatement_StaticMethod_PartialMethod()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System.Text.RegularExpressions;

            var result = {|MA0110:Regex.IsMatch("test", "pattern")|};
            """;
        test.FixedCode = """
            using System.Text.RegularExpressions;

            var result = MyRegex().IsMatch("test");

            partial class Program
            {
                [GeneratedRegex("pattern")]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task BatchFix_MultipleRegex()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.TestCode = """
            using System;
            using System.Text.RegularExpressions;

            class Test1
            {
                Regex a = {|MA0110:new Regex("pattern1")|};
            }

            class Test2
            {
                Regex b = {|MA0110:new Regex("pattern2")|};
            }
            """;
        test.FixedCode = """
            using System;
            using System.Text.RegularExpressions;

            partial class Test1
            {
                Regex a = MyRegex();

                [GeneratedRegex("pattern1")]
                private static partial Regex MyRegex();
            }

            partial class Test2
            {
                Regex b = MyRegex();

                [GeneratedRegex("pattern2")]
                private static partial Regex MyRegex();
            }
            """;

        return test.RunAsync();
    }}

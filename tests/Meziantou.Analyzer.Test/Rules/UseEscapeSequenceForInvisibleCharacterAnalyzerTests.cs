using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseEscapeSequenceForInvisibleCharacterAnalyzer,
    Meziantou.Analyzer.Rules.UseEscapeSequenceForInvisibleCharacterFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseEscapeSequenceForInvisibleCharacterAnalyzerTests
{
    private const string Tab = "\t";
    private const string Bell = "\a";
    private const string Nbsp = "\u00A0";
    private const string ZeroWidthSpace = "\u200B";
    private const string ZeroWidthJoiner = "\u200D";
    private const string RightToLeftOverride = "\u202E";
    private const string LineSeparator = "\u2028";
    private const string VariationSelector16 = "\uFE0F";
    private const string LanguageTag = "\U000E0001";
    private const string TagLatinSmallLetterG = "\U000E0067";

    [Fact]
    public Task Valid()
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            class Dummy
            {
                void Test()
                {
                    _ = "test";
                    _ = "a\u00A0b\tc";
                    _ = '\u200B';
                    _ = @"line1
            line2";
                    _ = "👨{{ZeroWidthJoiner}}👩";
                    _ = "❤{{VariationSelector16}}";
                    _ = "🏴{{TagLatinSmallLetterG}}";
                    _ = $"a{"\u00A0"}b";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RawString_Indentation()
    {
        var test = new CodeFixTest();
        test.TestCode = $$""""
            class Dummy
            {
                void Test()
                {
                    _ = """{{Tab}}
            {{Tab}}{{Tab}}a
            {{Tab}}
            {{Tab}}{{Tab}}b
            {{Tab}}{{Tab}}""";
                    _ = $"""{{Tab}}
            {{Tab}}{{Tab}}a{0}
            {{Tab}}{{Tab}}""";
                    _ = """{{Tab}}
            {{Tab}}{{Tab}}a
            {{Tab}}{{Tab}}"""u8;
                }
            }
            """";

        return test.RunAsync();
    }

    [Fact]
    public Task StringLiteral()
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            class Dummy
            {
                void Test()
                {
                    _ = "a[|{{Nbsp}}|]b";
                }
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = "a\u00A0b";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringLiteral_SimpleEscapeSequences()
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            class Dummy
            {
                void Test()
                {
                    _ = "a[|{{Tab}}|]b[|{{Bell}}|]c";
                }
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = "a\tb\ac";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringLiteral_SupplementaryCharacter()
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            class Dummy
            {
                void Test()
                {
                    _ = "a[|{{LanguageTag}}|]b";
                }
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = "a\U000E0001b";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringLiteral_FixAll()
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            class Dummy
            {
                void Test()
                {
                    _ = "a[|{{Nbsp}}|]b[|{{ZeroWidthSpace}}|]c[|{{RightToLeftOverride}}|]";
                    _ = "[|{{Nbsp}}|]" + "[|{{Nbsp}}|]";
                }
            }
            """;
        var fixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = "a\u00A0b\u200Bc\u202E";
                    _ = "\u00A0" + "\u00A0";
                }
            }
            """;
        test.FixedCode = fixedCode;
        test.BatchFixedCode = fixedCode;

        return test.RunAsync();
    }

    [Fact]
    public Task CharLiteral()
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            class Dummy
            {
                void Test()
                {
                    _ = '[|{{Tab}}|]';
                    _ = '[|{{Nbsp}}|]';
                }
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = '\t';
                    _ = '\u00A0';
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Utf8StringLiteral()
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            class Dummy
            {
                void Test()
                {
                    _ = "a[|{{ZeroWidthSpace}}|]b"u8;
                }
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = "a\u200Bb"u8;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedString()
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            class Dummy
            {
                void Test()
                {
                    _ = $"a[|{{Nbsp}}|]{{"{{"}}x{{"}}"}}{0}b[|{{RightToLeftOverride}}|]{"c":X}";
                }
            }
            """;
        var fixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = $"a\u00A0{{x}}{0}b\u202E{"c":X}";
                }
            }
            """;
        test.FixedCode = fixedCode;
        test.BatchFixedCode = fixedCode;

        return test.RunAsync();
    }

    [Fact]
    public Task VerbatimString()
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            class Dummy
            {
                void Test()
                {
                    _ = @"a""b\c[|{{Nbsp}}|]{{"\n"}}d[|{{LineSeparator}}|]e";
                }
            }
            """;
        var fixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = "a\"b\\c\u00A0\nd\u2028e";
                }
            }
            """;
        test.FixedCode = fixedCode;
        test.BatchFixedCode = fixedCode;

        return test.RunAsync();
    }

    [Fact]
    public Task VerbatimUtf8String()
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            class Dummy
            {
                void Test()
                {
                    _ = @"a\[|{{Nbsp}}|]"u8;
                }
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = "a\\\u00A0"u8;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VerbatimInterpolatedString()
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            class Dummy
            {
                void Test()
                {
                    _ = $@"a""{0}\[|{{Nbsp}}|]{"b"}{{"{{"}}";
                    _ = @$"a[|{{Tab}}|]{0}";
                }
            }
            """;
        test.FixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = $"a\"{0}\\\u00A0{"b"}{{";
                    _ = $"a\t{0}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VerbatimInterpolatedString_FormatWithBackslash_NoFix()
    {
        var test = new CodeFixTest();
        var sourceCode = $$"""
            class Dummy
            {
                void Test()
                {
                    _ = $@"{0:0\t}[|{{Nbsp}}|]";
                }
            }
            """;
        test.TestCode = sourceCode;
        test.FixedCode = sourceCode;

        return test.RunAsync();
    }

    [Fact]
    public Task RawString_NoFix()
    {
        var test = new CodeFixTest();
        var sourceCode = $$""""
            class Dummy
            {
                void Test()
                {
                    _ = """a[|{{Nbsp}}|]b""";
                    _ = """
                        a[|{{Tab}}|]b
                        [|{{Nbsp}}|]c
                        """;
                    _ = $"""
                        a[|{{ZeroWidthSpace}}|]{0}
                        """;
                    _ = """a[|{{Nbsp}}|]b"""u8;
                }
            }
            """";
        test.TestCode = sourceCode;
        test.FixedCode = sourceCode;

        return test.RunAsync();
    }
}

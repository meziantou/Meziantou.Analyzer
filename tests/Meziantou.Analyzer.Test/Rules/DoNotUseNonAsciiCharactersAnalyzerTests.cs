using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseNonAsciiCharactersAnalyzer>;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.DoNotUseNonAsciiCharactersAnalyzer,
    Meziantou.Analyzer.Rules.DoNotUseNonAsciiCharactersFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseNonAsciiCharactersAnalyzerTests
{
    private const string Nbsp = "\u00A0";
    private const string ZeroWidthSpace = "\u200B";
    private const string IdeographicSpace = "\u3000";
    private const string FormFeed = "\f";
    private const string Flag = "\U0001F1EB\U0001F1F7";

    private static Task VerifyAsync(string code, params (string Key, string Value)[] configuration)
    {
        var test = new AnalyzerTest { TestCode = code };
        SetConfiguration(test.TestState, configuration);
        return test.RunAsync();
    }

    private static Task VerifyFixAsync(string code, string fixedCode, params (string Key, string Value)[] configuration)
    {
        var test = new CodeFixTest { TestCode = code, FixedCode = fixedCode };
        SetConfiguration(test.TestState, configuration);
        return test.RunAsync();
    }

    private static void SetConfiguration(ProjectState state, (string Key, string Value)[] configuration)
    {
        if (configuration.Length > 0)
        {
            state.SetConfiguration([.. configuration.Select(item => ("MA0243." + item.Key, item.Value))]);
        }
    }

    private static DiagnosticResult Expected(string element, string codePoints) =>
        new DiagnosticResult(RuleIdentifiers.DoNotUseNonAsciiCharacters, DiagnosticSeverity.Info)
            .WithLocation(0)
            .WithMessage($"The {element} contains disallowed characters: {codePoints}");

    [Fact]
    public Task AsciiOnly_NoDiagnostic()
        => VerifyAsync(""""
            /// <summary>Sample</summary>
            class Sample
            {
                // Comment
                /* Comment */
                string A = "abc";
                string B = @"abc";
                string C = """abc""";
                char D = 'a';
            #region Region
            #endregion
            }
            """");

    [Theory]
    [InlineData("""string A = "[|é|]";""")]
    [InlineData("""string A = @"[|é|]";""")]
    [InlineData("""string A = $"[|é|]{1}";""")]
    [InlineData("""string A = $@"[|é|]{1}";""")]
    [InlineData("""char A = '[|é|]';""")]
    [InlineData("""int caf[|é|];""")]
    [InlineData("""// [|é|]""")]
    [InlineData("""/* [|é|] */""")]
    [InlineData("""/// <summary>[|é|]</summary>""" + "\nint A;")]
    [InlineData("""/// <see cref="Caf[|é|]"/>""" + "\nint A;")]
    [InlineData("#region [|é|]\n#endregion")]
    [InlineData("#if NEVER\n[|é|]\n#endif")]
    public Task Element_Diagnostic(string member)
        => VerifyAsync($$"""
            class Sample
            {
                {{member}}
            }
            """);

    [Fact]
    public Task Utf8String_Diagnostic()
        => VerifyAsync("""
            class Sample
            {
                void A()
                {
                    var a = "[|é|]"u8;
                    var b = @"[|é|]"u8;
                }
            }
            """);

    [Fact]
    public Task NonBreakingSpace_Diagnostic()
        => VerifyAsync($$"""
            class Sample
            {
                int[|{{Nbsp}}|]A;
            }
            """);

    [Fact]
    public Task AsciiControlCharacter_Diagnostic()
        => VerifyAsync($$"""
            class Sample
            {
            }
            [|{{FormFeed}}|]
            """);

    [Theory]
    [InlineData("""string A = "{|#0:é|}";""", "string literal", "U+00E9")]
    [InlineData("""string A = "{|#0:éà|}";""", "string literal", "U+00E9, U+00E0")]
    [InlineData("string A = \"e{|#0:\u0301|}\";", "string literal", "U+0301")]
    [InlineData("""string A = "{|#0:😀|}";""", "string literal", "U+1F600")]
    [InlineData("""string A = @"{|#0:é|}";""", "verbatim string literal", "U+00E9")]
    [InlineData("""char A = '{|#0:é|}';""", "character literal", "U+00E9")]
    [InlineData("""int caf{|#0:é|};""", "identifier", "U+00E9")]
    [InlineData("""// {|#0:é|}""", "comment", "U+00E9")]
    [InlineData("""/// <summary>{|#0:é|}</summary>""" + "\nint A;", "documentation comment", "U+00E9")]
    [InlineData("#region {|#0:é|}\n#endregion", "preprocessor directive", "U+00E9")]
    [InlineData("#if NEVER\n{|#0:é|}\n#endif", "disabled code", "U+00E9")]
    [InlineData("int{|#0:\u00A0|}A;", "whitespace", "U+00A0")]
    public Task Message(string member, string element, string codePoints)
    {
        var test = new AnalyzerTest
        {
            TestCode = $$"""
                class Sample
                {
                    {{member}}
                }
                """,
        };
        test.ExpectedDiagnostics.Add(Expected(element, codePoints));
        return test.RunAsync();
    }

    [Fact]
    public Task RawString_MultilineAndUtf8_Diagnostic()
        => VerifyAsync(""""
            class Sample
            {
                void A()
                {
                    var a = """[|é|]""";
                    var b = """
                        [|é|]
                        """;
                    var c = """[|é|]"""u8;
                    var d = $"""[|é|]{1}""";
                }
            }
            """", ("skip_raw_strings", "false"));

    [Fact]
    public Task Runs_SeparatedCharactersAndElements()
        => VerifyAsync("""
            class Sample
            {
                string A = "[|é|]a[|à|]";
                int caf[|é|]/*[|é|]*/;
            }
            """);

    [Theory]
    [InlineData("""string A = "é";""", "skip_strings")]
    [InlineData("""string A = $"é{1}";""", "skip_strings")]
    [InlineData("""string A = @"é";""", "skip_verbatim_strings")]
    [InlineData("""string A = $@"é{1}";""", "skip_verbatim_strings")]
    [InlineData("""string A = @$"é{1}";""", "skip_verbatim_strings")]
    [InlineData("""char A = 'é';""", "skip_chars")]
    [InlineData("""int café;""", "skip_identifiers")]
    [InlineData("""// é""", "skip_comments")]
    [InlineData("""/* é */""", "skip_comments")]
    [InlineData("""/// <summary>é</summary>""" + "\nint A;", "skip_documentation_comments")]
    [InlineData("""/** <summary>é</summary> */""" + "\nint A;", "skip_documentation_comments")]
    public Task SkipOption_NoDiagnostic(string member, string option)
        => VerifyAsync($$"""
            class Sample
            {
                {{member}}
            }
            """, (option, "true"));

    [Theory]
    [InlineData("""string A = "[|é|]";""", "skip_verbatim_strings")]
    [InlineData("""string A = "[|é|]";""", "skip_chars")]
    [InlineData("""string A = @"[|é|]";""", "skip_strings")]
    [InlineData("""string A = $@"[|é|]{1}";""", "skip_strings")]
    [InlineData("""string A = @"[|é|]";""", "skip_chars")]
    [InlineData("""char A = '[|é|]';""", "skip_strings")]
    [InlineData("""char A = '[|é|]';""", "skip_verbatim_strings")]
    [InlineData("""string A = "[|é|]";""", "skip_identifiers")]
    [InlineData("""/// <summary>[|é|]</summary>""" + "\nint A;", "skip_comments")]
    [InlineData("""// [|é|]""", "skip_documentation_comments")]
    public Task SkipOption_OtherElement_Diagnostic(string member, string option)
        => VerifyAsync($$"""
            class Sample
            {
                {{member}}
            }
            """, (option, "true"));

    [Fact]
    public Task SkipStrings_Utf8()
        => VerifyAsync("""
            class Sample
            {
                void A()
                {
                    var a = "é"u8;
                    var b = @"[|é|]"u8;
                }
            }
            """, ("skip_strings", "true"));

    [Fact]
    public Task SkipVerbatimStrings_Utf8()
        => VerifyAsync("""
            class Sample
            {
                void A()
                {
                    var a = "[|é|]"u8;
                    var b = @"é"u8;
                }
            }
            """, ("skip_verbatim_strings", "true"));

    [Fact]
    public Task RawString_SkippedByDefault()
        => VerifyAsync(""""
            class Sample
            {
                void A()
                {
                    var a = """é""";
                    var b = """
                        é
                        """;
                    var c = $"""é{1}""";
                    var d = $"""
                        é{1}
                        """;
                }
            }
            """");

    [Theory]
    [InlineData(Nbsp)]
    [InlineData(IdeographicSpace)]
    [InlineData(Nbsp + IdeographicSpace)]
    public Task Indentation_Diagnostic(string indentation)
        => VerifyAsync($$"""
            class Sample
            {
            [|{{indentation}}|]int A;
            }
            """);

    [Theory]
    [InlineData(Nbsp)]
    [InlineData(IdeographicSpace)]
    [InlineData(Nbsp + "\t" + IdeographicSpace)]
    public Task SkipIndentation_NoDiagnostic(string indentation)
        => VerifyAsync($$"""
            class Sample
            {
            {{indentation}}int A;
            {{indentation}}// comment
            }
            """, ("skip_indentation", "true"));

    [Fact]
    public Task SkipIndentation_WhitespaceAfterCode_Diagnostic()
        => VerifyAsync($$"""
            class Sample
            {
                int[|{{Nbsp}}|]A;
            }
            """, ("skip_indentation", "true"));

    [Fact]
    public Task SkipIndentation_WhitespaceInLiterals_Diagnostic()
        => VerifyAsync($$""""
            class Sample
            {
                string A = @"
            [|{{Nbsp}}|]text";
                string B = """
                [|{{Nbsp}}|]text
                """;
            }
            """", ("skip_indentation", "true"), ("skip_raw_strings", "false"));

    [Theory]
    [InlineData("U+00E9")]
    [InlineData("u+00e9")]
    [InlineData("00E9")]
    [InlineData("U+00C0-U+00FF")]
    [InlineData("00C0-00FF")]
    [InlineData("invalid, U+00E9")]
    [InlineData(" U+0100 , U+00E9 ")]
    [InlineData("U+0100,U+00E9")]
    [InlineData("U+00F0-U+00FF, U+00E0-U+00F5, U+00E9")]
    public Task AdditionalAllowedRanges_NoDiagnostic(string ranges)
        => VerifyAsync("""
            class Sample
            {
                string A = "é";
            }
            """, ("additional_allowed_ranges", ranges));

    [Theory]
    [InlineData("U+00E0")]
    [InlineData("invalid")]
    [InlineData("U+00FF-U+00C0")]
    [InlineData("U+110000")]
    public Task AdditionalAllowedRanges_Diagnostic(string ranges)
        => VerifyAsync("""
            class Sample
            {
                string A = "[|é|]";
            }
            """, ("additional_allowed_ranges", ranges));

    [Theory]
    [InlineData("\U0001F600")]
    [InlineData("\u2764\uFE0F")]
    [InlineData("\U0001F44D\U0001F3FD")]
    [InlineData("\U0001F468\u200D\U0001F469\u200D\U0001F467")]
    [InlineData("\U0001F1EB\U0001F1F7")]
    [InlineData("1\uFE0F\u20E3")]
    [InlineData("#\u20E3")]
    [InlineData("\U0001F3F4\U000E0067\U000E0062\U000E0065\U000E006E\U000E0067\U000E007F")]
    public Task AllowEmoji_NoDiagnostic(string emoji)
        => VerifyAsync($$"""
            class Sample
            {
                // {{emoji}}
                string A = "{{emoji}}";
            }
            """, ("allow_emoji", "true"));

    [Fact]
    public Task AllowEmoji_OtherCharacters_Diagnostic()
        => VerifyAsync($$"""
            class Sample
            {
                string A = "😀[|é|]";
                string B = "a[|{{ZeroWidthSpace}}|]";
            }
            """, ("allow_emoji", "true"));

    [Fact]
    public Task Emoji_Diagnostic()
        => VerifyAsync($$"""
            class Sample
            {
                string A = "[|😀|]";
                string B = "[|{{Flag}}|]";
            }
            """);

    [Fact]
    public Task AllowVisibleCharacters()
        => VerifyAsync($$"""
            class Sample
            {
                string A = "é€😀";
                string B = "[|{{Nbsp}}|]";
                string C = "a[|{{ZeroWidthSpace}}|]";
            }
            """, ("allow_visible_characters", "true"));

    [Fact]
    public Task AlwaysReportInvisibleCharacters_InSkippedElements_Diagnostic()
        => VerifyAsync($$""""
            class Sample
            {
                // a[|{{ZeroWidthSpace}}|]é
                string A = """a[|{{ZeroWidthSpace}}|]é""";
            }
            """", ("skip_comments", "true"));

    [Fact]
    public Task AlwaysReportInvisibleCharacters_Disabled_NoDiagnostic()
        => VerifyAsync($$""""
            class Sample
            {
                // a{{ZeroWidthSpace}}é
                string A = """a{{ZeroWidthSpace}}é""";
            }
            """", ("skip_comments", "true"), ("always_report_invisible_characters", "false"));

    [Fact]
    public Task AlwaysReportInvisibleCharacters_InAllowedRange_Diagnostic()
        => VerifyAsync($$"""
            class Sample
            {
                string A = "[|{{Nbsp}}|]";
            }
            """, ("additional_allowed_ranges", "U+00A0"));

    [Fact]
    public Task AlwaysReportInvisibleCharacters_InAllowedRange_Disabled_NoDiagnostic()
        => VerifyAsync($$"""
            class Sample
            {
                string A = "{{Nbsp}}";
            }
            """, ("additional_allowed_ranges", "U+00A0"), ("always_report_invisible_characters", "false"));

    [Fact]
    public Task AlwaysReportInvisibleCharacters_Disabled_NotSkipped_Diagnostic()
        => VerifyAsync($$"""
            class Sample
            {
                string A = "[|{{Nbsp}}|]";
            }
            """, ("always_report_invisible_characters", "false"));

    [Theory]
    [InlineData("""string A = "[|é|]";""", """string A = "\u00E9";""")]
    [InlineData("""string A = "[|😀|]";""", """string A = "\U0001F600";""")]
    [InlineData("""string A = "[|éà|]b";""", """string A = "\u00E9\u00E0b";""")]
    [InlineData("""string A = "[|é|]a[|à|]";""", """string A = "\u00E9a\u00E0";""")]
    [InlineData("""char A = '[|é|]';""", """char A = '\u00E9';""")]
    [InlineData("""string A = $"[|é|]{1}";""", """string A = $"\u00E9{1}";""")]
    [InlineData("string A = \"[|\u00A0|]\";", """string A = "\u00A0";""")]
    public Task CodeFix(string member, string fixedMember)
        => VerifyFixAsync($$"""
            class Sample
            {
                {{member}}
            }
            """, $$"""
            class Sample
            {
                {{fixedMember}}
            }
            """);

    [Fact]
    public Task CodeFix_Utf8String()
        => VerifyFixAsync("""
            class Sample
            {
                void A()
                {
                    var a = "[|é|]"u8;
                }
            }
            """, """
            class Sample
            {
                void A()
                {
                    var a = "\u00E9"u8;
                }
            }
            """);

    [Fact]
    public Task CodeFix_OnlyDisallowedCharacters()
        => VerifyFixAsync("""
            class Sample
            {
                string A = "é[|à|]";
            }
            """, """
            class Sample
            {
                string A = "é\u00E0";
            }
            """, ("additional_allowed_ranges", "U+00E9"));

    [Theory]
    [InlineData("""string A = @"[|é|]";""")]
    [InlineData("""string A = $@"[|é|]{1}";""")]
    [InlineData("""string A = $"{1:[|é|]}";""")]
    [InlineData("string A = \"\"\"[|é|]\"\"\";")]
    [InlineData("""int caf[|é|];""")]
    [InlineData("""// [|é|]""")]
    public Task CodeFix_NotSupported(string member)
    {
        var code = $$"""
            class Sample
            {
                {{member}}
            }
            """;
        return VerifyFixAsync(code, code, ("skip_raw_strings", "false"));
    }
}

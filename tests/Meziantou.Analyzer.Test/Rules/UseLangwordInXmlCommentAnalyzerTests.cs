using Meziantou.Analyzer.Internals;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseLangwordInXmlCommentAnalyzer,
    Meziantou.Analyzer.Rules.UseLangwordInXmlCommentFixer>;
using AddLanguageAttributeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseLangwordInXmlCommentAnalyzer,
    Meziantou.Analyzer.Rules.UseLangwordInXmlCommentAddLanguageAttributeFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseLangwordInXmlCommentAnalyzerTests
{
    // MA0219 is reported by its own analyzer, so the tests run both analyzers
    private static CodeFixTest CreateTest() => new() { AdditionalAnalyzers = { new MissingLanguageAttributeInXmlCommentAnalyzer() } };

    private static AddLanguageAttributeFixTest CreateAddLanguageAttributeFixTest() => new() { AdditionalAnalyzers = { new MissingLanguageAttributeInXmlCommentAnalyzer() } };

    [Theory]
    [InlineData("{|MA0154:<c>void</c>|}", "<see langword=\"void\"/>")]
    [InlineData("{|MA0154:<code>void</code>|}", "<see langword=\"void\"/>")]
    [InlineData("{|MA0154:<code>null</code>|}", "<see langword=\"null\"/>")]
    [InlineData("{|MA0154:<c>async</c>|}", "<see langword=\"async\"/>")]
    [InlineData("{|MA0154:<c>await</c>|}", "<see langword=\"await\"/>")]
    [InlineData("{|MA0154:<c>dynamic</c>|}", "<see langword=\"dynamic\"/>")]
    [InlineData("{|MA0154:<c>init</c>|}", "<see langword=\"init\"/>")]
    [InlineData("{|MA0154:<c>nameof</c>|}", "<see langword=\"nameof\"/>")]
    [InlineData("{|MA0154:<c>nint</c>|}", "<see langword=\"nint\"/>")]
    [InlineData("{|MA0154:<c>nuint</c>|}", "<see langword=\"nuint\"/>")]
    [InlineData("{|MA0154:<c>partial</c>|}", "<see langword=\"partial\"/>")]
    [InlineData("{|MA0154:<c>record</c>|}", "<see langword=\"record\"/>")]
    [InlineData("{|MA0154:<c>required</c>|}", "<see langword=\"required\"/>")]
    [InlineData("{|MA0154:<c>scoped</c>|}", "<see langword=\"scoped\"/>")]
    [InlineData("{|MA0154:<c>var</c>|}", "<see langword=\"var\"/>")]
    public Task ValidateSummary_Invalid(string comment, string fix)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            /// <summary>{{comment}}</summary>
            class Sample { }
            """;
        test.FixedCode = $$"""
            /// <summary>{{fix}}</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("<i>in</i>")]
    [InlineData("null")]
    [InlineData("this is null")]
    [InlineData("<c language=\"json\">null</c>")]
    public Task ValidateSummary_Valid(string comment)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            /// <summary>{{comment}}</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Theory]
    // The fix adds an empty 'language' attribute for the user to fill in, which MA0218 then reports
    [InlineData("{|MA0154:<c>void</c>|}", "<c {|MA0218:language=\"\"|}>void</c>")]
    [InlineData("{|MA0154:<code>void</code>|}", "<code {|MA0218:language=\"\"|}>void</code>")]
    public Task AddLanguageAttribute(string comment, string fix)
    {
        var test = CreateAddLanguageAttributeFixTest();
        test.TestCode = $$"""
            /// <summary>{{comment}}</summary>
            class Sample { }
            """;
        test.FixedCode = $$"""
            /// <summary>{{fix}}</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("""<c {|MA0218:lang=""|}>test</c>""")]
    [InlineData("""<c {|MA0218:lang=" "|}>test</c>""")]
    [InlineData("""<code {|MA0218:language=""|}>test</code>""")]
    [InlineData("""<c {|MA0218:lang=""|}>void</c>""")]
    public Task EmptyLanguageAttribute(string comment)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            /// <summary>{{comment}}</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("""<c lang="csharp">test</c>""")]
    [InlineData("""<c language="json">test</c>""")]
    [InlineData("{|MA0219:<c>test</c>|}")]
    public Task EmptyLanguageAttribute_Valid(string comment)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            /// <summary>{{comment}}</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("{|MA0219:<c>test</c>|}")]
    [InlineData("{|MA0219:<code>test</code>|}")]
    [InlineData("""{|MA0219:<c title="sample">test</c>|}""")]
    // Contextual keywords that are commonly used as identifiers or plain words are not reported as keywords
    [InlineData("{|MA0219:<c>value</c>|}")]
    [InlineData("{|MA0219:<c>from</c>|}")]
    [InlineData("{|MA0219:<c>get</c>|}")]
    [InlineData("{|MA0219:<c>set</c>|}")]
    public Task MissingLanguageAttribute(string comment)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            /// <summary>{{comment}}</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("""<c lang="csharp">test</c>""")]
    [InlineData("""<c {|MA0218:language=""|}>test</c>""")]
    [InlineData("""<code language="json">test</code>""")]
    [InlineData("""<c langword="null"></c>""")]
    [InlineData("{|MA0154:<c>void</c>|}")]
    [InlineData("<see langword=\"null\"/>")]
    public Task MissingLanguageAttribute_Valid(string comment)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            /// <summary>{{comment}}</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingLanguageAttribute_Fix()
    {
        var test = CreateAddLanguageAttributeFixTest();
        test.TestCode = """
            /// <summary>{|MA0219:<c>test</c>|}</summary>
            class Sample { }
            """;
        test.FixedCode = """
            /// <summary><c {|MA0218:language=""|}>test</c></summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// <summary>{|MA0154:<c>null</c>|}</summary>
                int a;
            }
            """;
        test.FixedCode = """
            class Sample
            {
                /// <summary><see langword="null"/></summary>
                int a;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FieldWithMultipleVariables()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// <summary>{|MA0154:<c>null</c>|}</summary>
                int a, b;
            }
            """;
        test.FixedCode = """
            class Sample
            {
                /// <summary><see langword="null"/></summary>
                int a, b;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RecordWithPrimaryConstructor()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary>{|MA0154:<c>null</c>|}</summary>
            record Sample(int A);
            """;
        test.FixedCode = """
            /// <summary><see langword="null"/></summary>
            record Sample(int A);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClassWithPrimaryConstructor_MissingLanguageAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary>{|MA0219:<c>test</c>|}</summary>
            class Sample(int a);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingLanguageAttributeAnalyzer_DoesNotReportKeywords()
    {
        var test = new Harness.CSharpAnalyzerTest<MissingLanguageAttributeInXmlCommentAnalyzer>();
        test.TestCode = """
            /// <summary><c>void</c> [|<c>test</c>|] <c language="">test</c></summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UseLangwordAnalyzer_DoesNotReportMissingLanguageAttribute()
    {
        var test = new Harness.CSharpAnalyzerTest<UseLangwordInXmlCommentAnalyzer>();
        test.TestCode = """
            /// <summary>{|MA0154:<c>void</c>|} <c>test</c> <c {|MA0218:language=""|}>test</c></summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("{|MA0154:<C>null</C>|}")]
    [InlineData("{|MA0154:<CODE>null</CODE>|}")]
    [InlineData("{|MA0154:<c >null</c>|}")]
    [InlineData("{|MA0219:<Code>test</Code>|}")]
    [InlineData("<para><b>{|MA0154:<c>null</c>|}</b></para>")]
    // The content of a code element is not analyzed
    [InlineData("{|MA0219:<code><c>null</c> test</code>|}")]
    [InlineData("<c/>")]
    [InlineData("<code />")]
    [InlineData("<![CDATA[<c>null</c>]]>")]
    [InlineData("<!-- <c>null</c> -->")]
    // An element of another XML namespace is not a code element
    [InlineData("""<x:c xmlns:x="urn:x">null</x:c>""")]
    public Task CodeElementSyntax(string comment)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            /// <summary>{{comment}}</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StartTagOnMultipleLines()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary>
            /// {|MA0154:<c
            /// >null</c>|}
            /// <code
            /// {|MA0218:language=""|}>
            /// test
            /// </code>
            /// </summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultiLineDocumentationComment()
    {
        var test = CreateTest();
        test.TestCode = """
            /**
             * <summary>{|MA0154:<c>null</c>|}</summary>
             * <remarks>
             * {|MA0219:<code>
             * test
             * </code>|}
             * </remarks>
             */
            class Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Members()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary>{|MA0154:<c>null</c>|}</summary>
            enum SampleEnum
            {
                /// <summary>{|MA0154:<c>null</c>|}</summary>
                A,
            }

            /// <summary>{|MA0154:<c>null</c>|}</summary>
            delegate void SampleDelegate();

            partial class Sample
            {
                /// <summary>{|MA0154:<c>null</c>|}</summary>
                Sample() { }

                /// <summary>{|MA0154:<c>null</c>|}</summary>
                event System.EventHandler A, B;

                /// <summary>{|MA0154:<c>null</c>|}</summary>
                int this[int index] => 0;

                int C
                {
                    /// <summary>{|MA0154:<c>null</c>|}</summary>
                    get => 0;
                }

                /// <summary>{|MA0154:<c>null</c>|}</summary>
                partial void D();

                /// <summary>{|MA0154:<c>null</c>|}</summary>
                partial void D() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SeveralCommentsOnTheSameMember()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                /// <summary>{|MA0154:<c>null</c>|} {|MA0219:<c>test</c>|}</summary>

                /// <remarks>{|MA0154:<c>true</c>|}</remarks>
                int a;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CommentNotPlacedOnTypeOrMember()
    {
        var test = CreateTest();
        test.TestCode = """
            /// <summary><c>null</c></summary>
            namespace Test
            {
                [System.Obsolete]
                /// <summary><c>null</c></summary>
                class Sample
                {
                    void A()
                    {
                        /// <summary><c>null</c></summary>
                        void B() { }
                    }

                    // <c>null</c>
                    /* <c>null</c> */
                    string C = "<c>null</c>";
                    System.Collections.Generic.List<C> D;

            #if DISABLED
                    /// <summary><c>null</c></summary>
                    int E;
            #endif

                    /// <summary><c>null</c></summary>
                }

                class C { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CommentBeforeTopLevelStatement()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            /// <summary><c>null</c></summary>
            System.Console.WriteLine();

            /// <summary>{|MA0154:<c>null</c>|}</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }

    // The text is read by chunks, and the last character of a chunk is read again by the next one.
    // The offset is relative to the end of the chunk, so the start of the element spans two chunks when the offset is -1.
    [Theory]
    [InlineData(1, -2)]
    [InlineData(1, -1)]
    [InlineData(1, 0)]
    [InlineData(2, -2)]
    [InlineData(2, -1)]
    [InlineData(2, 0)]
    public Task CodeElementAtTheEndOfAReadBuffer(int bufferCount, int offset)
    {
        const string CommentStart = "/// <summary>";
        var endOfBuffer = (bufferCount * SourceTextSearcher.ReadBufferSize) - (bufferCount - 1);
        var elementPosition = endOfBuffer + offset;

        var test = CreateTest();
        test.TestCode = new string(' ', elementPosition - CommentStart.Length) + CommentStart + """
            {|MA0154:<c>null</c>|} {|MA0219:<code>test</code>|}</summary>
            class Sample { }
            """;

        return test.RunAsync();
    }
}

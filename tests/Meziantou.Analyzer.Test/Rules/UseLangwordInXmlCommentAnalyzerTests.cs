using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseLangwordInXmlCommentAnalyzer,
    Meziantou.Analyzer.Rules.UseLangwordInXmlCommentFixer>;
using AddLanguageAttributeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseLangwordInXmlCommentAnalyzer,
    Meziantou.Analyzer.Rules.UseLangwordInXmlCommentAddLanguageAttributeFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseLangwordInXmlCommentAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    private static AddLanguageAttributeFixTest CreateAddLanguageAttributeFixTest() => new();

    [Theory]
    [InlineData("{|MA0154:<c>void</c>|}", "<see langword=\"void\"/>")]
    [InlineData("{|MA0154:<code>void</code>|}", "<see langword=\"void\"/>")]
    [InlineData("{|MA0154:<code>null</code>|}", "<see langword=\"null\"/>")]
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
}

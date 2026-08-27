namespace Meziantou.Analyzer.Test.Rules;
public sealed class UseLangwordInXmlCommentAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseLangwordInXmlCommentAnalyzer>()
            .WithCodeFixProvider<UseLangwordInXmlCommentFixer>()
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    private static ProjectBuilder CreateProjectBuilder(string ruleId)
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseLangwordInXmlCommentAnalyzer>(id: ruleId)
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    private static ProjectBuilder CreateProjectBuilderWithAddLanguageAttributeFixer()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseLangwordInXmlCommentAnalyzer>()
            .WithCodeFixProvider<UseLangwordInXmlCommentAddLanguageAttributeFixer>()
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    [Theory]
    [InlineData("[|<c>void</c>|]", "<see langword=\"void\"/>")]
    [InlineData("[|<code>void</code>|]", "<see langword=\"void\"/>")]
    [InlineData("[|<code>null</code>|]", "<see langword=\"null\"/>")]
    public async Task ValidateSummary_Invalid(string comment, string fix)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
/// <summary>{{comment}}</summary>
class Sample { }
""")
              .ShouldFixCodeWith($$"""
/// <summary>{{fix}}</summary>
class Sample { }
""")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("<i>in</i>")]
    [InlineData("null")]
    [InlineData("this is null")]
    [InlineData("<c language=\"json\">null</c>")]
    public async Task ValidateSummary_Valid(string comment)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
/// <summary>{{comment}}</summary>
class Sample { }
""")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("[|<c>void</c>|]", "<c {|MA0218:language=\"\"|}>void</c>")]
    [InlineData("[|<code>void</code>|]", "<code {|MA0218:language=\"\"|}>void</code>")]
    public async Task AddLanguageAttribute(string comment, string fix)
    {
        await CreateProjectBuilderWithAddLanguageAttributeFixer()
              .WithSourceCode($$"""
/// <summary>{{comment}}</summary>
class Sample { }
""")
              .ShouldFixCodeWith($$"""
/// <summary>{{fix}}</summary>
class Sample { }
""")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("""<c [|lang=""|]>test</c>""")]
    [InlineData("""<c [|lang=" "|]>test</c>""")]
    [InlineData("""<code [|language=""|]>test</code>""")]
    [InlineData("""<c [|lang=""|]>void</c>""")]
    public async Task EmptyLanguageAttribute(string comment)
    {
        await CreateProjectBuilder("MA0218")
              .WithSourceCode($$"""
/// <summary>{{comment}}</summary>
class Sample { }
""")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("""<c lang="csharp">test</c>""")]
    [InlineData("""<c language="json">test</c>""")]
    [InlineData("<c>test</c>")]
    public async Task EmptyLanguageAttribute_Valid(string comment)
    {
        await CreateProjectBuilder("MA0218")
              .WithSourceCode($$"""
/// <summary>{{comment}}</summary>
class Sample { }
""")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("[|<c>test</c>|]")]
    [InlineData("[|<code>test</code>|]")]
    [InlineData("""[|<c title="sample">test</c>|]""")]
    public async Task MissingLanguageAttribute(string comment)
    {
        await CreateProjectBuilder("MA0219")
              .WithSourceCode($$"""
/// <summary>{{comment}}</summary>
class Sample { }
""")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("""<c lang="csharp">test</c>""")]
    [InlineData("""<c language="">test</c>""")]
    [InlineData("""<code language="json">test</code>""")]
    [InlineData("""<c langword="null"></c>""")]
    [InlineData("<c>void</c>")]
    [InlineData("<see langword=\"null\"/>")]
    public async Task MissingLanguageAttribute_Valid(string comment)
    {
        await CreateProjectBuilder("MA0219")
              .WithSourceCode($$"""
/// <summary>{{comment}}</summary>
class Sample { }
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task MissingLanguageAttribute_Fix()
    {
        await new ProjectBuilder()
              .WithAnalyzer<UseLangwordInXmlCommentAnalyzer>(id: "MA0219")
              .WithCodeFixProvider<UseLangwordInXmlCommentAddLanguageAttributeFixer>()
              .WithTargetFramework(TargetFramework.NetLatest)
              .WithSourceCode(""""
/// <summary>[|<c>test</c>|]</summary>
class Sample { }
"""")
              .ShouldFixCodeWith(""""
/// <summary><c language="">test</c></summary>
class Sample { }
"""")
              .ValidateAsync();
    }
}

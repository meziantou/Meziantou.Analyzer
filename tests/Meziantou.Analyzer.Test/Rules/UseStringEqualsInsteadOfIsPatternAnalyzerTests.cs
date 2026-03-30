using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringEqualsInsteadOfIsPatternAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseStringEqualsInsteadOfIsPatternAnalyzer>()
            .WithCodeFixProvider<UseStringEqualsInsteadOfIsPatternFixer>();
    }

    [Fact]
    public async Task IsStringEmpty()
    {
        const string SourceCode = """
class TypeName
{
    public void Test(string str)
    {
        _ = str is "";
    }
}
""";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task IsNull()
    {
        const string SourceCode = """
class TypeName
{
    public void Test(string str)
    {
        _ = str is null;
    }
}
""";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task IsNotNull()
    {
        const string SourceCode = """
class TypeName
{
    public void Test(string str)
    {
        _ = str is not null;
    }
}
""";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task PatternMatching()
    {
        const string SourceCode = """
class TypeName
{
    public void Test(string str)
    {
        _ = str is [|"b"|];
    }
}
""";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task PatternMatching_CodeFix_Ordinal()
    {
        const string SourceCode = """
class TypeName
{
    public void Test(string str)
    {
        _ = str is [|"b"|];
    }
}
""";

        const string FixedCode = """
class TypeName
{
    public void Test(string str)
    {
        _ = string.Equals(str, "b", System.StringComparison.Ordinal);
    }
}
""";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(0, FixedCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task PatternMatching_CodeFix_OrdinalIgnoreCase()
    {
        const string SourceCode = """
class TypeName
{
    public void Test(string str)
    {
        _ = str is [|"b"|];
    }
}
""";

        const string FixedCode = """
class TypeName
{
    public void Test(string str)
    {
        _ = string.Equals(str, "b", System.StringComparison.OrdinalIgnoreCase);
    }
}
""";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(1, FixedCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task PatternMatching_Complex1()
    {
        const string SourceCode = """
class TypeName
{
    string Value { get; set; }

    public void Test(TypeName obj)
    {
        _ = obj is { Value: [|"b"|]};
    }
}
""";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task PatternMatching_Complex2()
    {
        const string SourceCode = """
class TypeName
{
    string Value { get; set; }

    public void Test(TypeName obj)
    {
        _ = obj is { Value: [|"b"|] or [|"c"|]};
    }
}
""";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task PatternMatching_Complex3()
    {
        const string SourceCode = """
class TypeName
{
    string Value { get; set; }

    public void Test(TypeName obj)
    {
        _ = obj is { Value: var a and ([|"b"|] or [|"c"|])};
    }
}
""";

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public void Rule_SeverityAndDefault()
    {
        var rule = new UseStringEqualsInsteadOfIsPatternAnalyzer().SupportedDiagnostics[0];
        Assert.Equal(DiagnosticSeverity.Hidden, rule.DefaultSeverity);
        Assert.True(rule.IsEnabledByDefault);
    }
}

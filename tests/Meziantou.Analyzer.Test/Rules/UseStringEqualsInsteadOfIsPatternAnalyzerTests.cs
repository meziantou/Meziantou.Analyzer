using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringEqualsInsteadOfIsPatternAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseStringEqualsInsteadOfIsPatternAnalyzer>();
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
}

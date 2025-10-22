using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class LocalVariablesShouldNotHideSymbolsAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<LocalVariablesShouldNotHideSymbolsAnalyzer>();
    }

    [Fact]
    public async Task LocalVariableHideField()
    {
        const string SourceCode = @"
class Test
{
    string a;

    void A()
    {
        var [|a|] = 10;
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LocalVariableHideProperty()
    {
        const string SourceCode = @"
class Test
{
    string Prop {get;set;}

    void A()
    {
        var [|Prop|] = 10;
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LocalVariableHideVisibleFieldFromParentClass()
    {
        const string SourceCode = @"
class Base
{
    protected string a;
}

class Test : Base
{
    void A()
    {
        var [|a|] = 10;
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

#if CSHARP12_OR_GREATER
    [Fact]
    public async Task LocalVariableHidePrimaryConstructorParameter()
    {
        const string SourceCode = """
            class Test(int a)
            {
                void A()
                {
                    var [|a|] = 10;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LocalVariableDoesNotHidePrimaryConstructorParameterInStaticMethod()
    {
        const string SourceCode = """
            class Test(int a)
            {
                static void A()
                {
                    var a = 10;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LocalVariableDoesNotHidePrimaryConstructorParameter()
    {
        const string SourceCode = """
            class Test(int a)
            {
                void A()
                {
                    var b = 10;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task LocalVariableHideNotVisibleFieldFromParentClass()
    {
        const string SourceCode = @"
class Base
{
    private string a;
}

class Test : Base
{
    void A()
    {
        var a = 10;
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LocalVariableDoesNotHideSymbol()
    {
        const string SourceCode = @"
class Test
{
    void A()
    {
        var a = 10;
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}

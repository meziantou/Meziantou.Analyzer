using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RemoveEmptyStatementAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<RemoveEmptyStatementAnalyzer>()
            .WithCodeFixProvider<RemoveEmptyStatementFixer>();
    }

    [Fact]
    public async Task EmptyStatement()
    {
        const string SourceCode = @"
class Test
{
    public void A()
    {
        [||];
    }
}";
        const string Fix = @"
class Test
{
    public void A()
    {
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task EmptyInLoopStatement()
    {
        const string SourceCode = @"
class Test
{
    public void A()
    {
        while(true)
        {
            [||];
        }
    }
}";
        const string Fix = @"
class Test
{
    public void A()
    {
        while(true)
        {
        }
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task WhileStatement()
    {
        const string SourceCode = @"
class Test
{
    public void A()
    {
        while(true)
            [||];
    }
}";
        const string Fix = @"
class Test
{
    public void A()
    {
        while(true)
        {
        }
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ForStatement()
    {
        const string SourceCode = @"
class Test
{
    public void A()
    {
        for(;;)
            [||];
    }
}";
        const string Fix = @"
class Test
{
    public void A()
    {
        for(;;)
        {
        }
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ForEachStatement()
    {
        const string SourceCode = @"
class Test
{
    public void A()
    {
        foreach(var a in new []{0})
            [||];
    }
}";
        const string Fix = @"
class Test
{
    public void A()
    {
        foreach(var a in new []{0})
        {
        }
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task EmptyStatementInALabel()
    {
        const string SourceCode = @"
class Test
{
    public void A()
    {
test:
        ;
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}

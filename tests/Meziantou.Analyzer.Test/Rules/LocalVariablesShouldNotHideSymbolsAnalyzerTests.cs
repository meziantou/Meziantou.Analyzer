using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
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
}

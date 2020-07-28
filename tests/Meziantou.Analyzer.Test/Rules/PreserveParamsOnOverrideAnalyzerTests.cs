using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class PreserveParamsOnOverrideAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<PreserveParamsOnOverrideAnalyzer>();
        }

        [Fact]
        public async Task MissingParamsFromBaseClass()
        {
            const string SourceCode = @"
class Test
{
    protected virtual void A(params string[] a) => throw null;
}

class Test2 : Test
{
    protected override void A(string[] [|a|]) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task MissingParamsFromInterface()
        {
            const string SourceCode = @"
interface ITest
{
    void A(params string[] a);
}

class Test2 : ITest
{
    public void A(string[] [|a|]) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task ParamsFromBaseClass()
        {
            const string SourceCode = @"
class Test
{
    protected virtual void A(params string[] a) => throw null;
}

class Test2 : Test
{
    protected override void A(params string[] a) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task ParamsFromInterface()
        {
            const string SourceCode = @"
interface ITest
{
    void A(params string[] a);
}

class Test2 : ITest
{
    public void A(params string[] a) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

    }
}

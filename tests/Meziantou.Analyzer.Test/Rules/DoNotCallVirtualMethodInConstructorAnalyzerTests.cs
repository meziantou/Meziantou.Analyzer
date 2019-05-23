using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class DoNotCallVirtualMethodInConstructorAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotCallVirtualMethodInConstructorAnalyzer>();
        }

        [TestMethod]
        public async Task CtorWithVirtualCall()
        {
            const string SourceCode = @"
class Test
{
    Test()
    {
        [|]A();
    }

    public virtual void A() { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task CtorWithNoVirtualCall()
        {
            const string SourceCode = @"
class Test
{
    Test()
    {
        A();
    }

    public void A() { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task CtorWithVirtualCallOnAnotherInstance()
        {
            const string SourceCode = @"
class Test
{
    Test()
    {
        var test = new Test();
        test.A();
    }

    public virtual void A() { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}

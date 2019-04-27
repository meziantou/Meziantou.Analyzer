using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class AbstractTypesShouldNotHaveConstructorsAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<AbstractTypesShouldNotHaveConstructorsAnalyzer>()
                .WithCodeFixProvider<AbstractTypesShouldNotHaveConstructorsFixer>();
        }

        [TestMethod]
        public async Task Ctor()
        {
            const string SourceCode = @"
abstract class Test
{
    protected Test(int a) { }
    private Test(object a) { }
}

class Test2
{
    public Test2() { }
    internal Test2(long a) { }
    protected Test2(int a) { }
    private Test2(object a) { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task PublicCtor()
        {
            var sourceCode = @"
abstract class Test
{
    public [|]Test() { }
}";

            var expectedCodeFix = @"
abstract class Test
{
    protected Test() { }
}";

            await CreateProjectBuilder()
                    .WithSourceCode(sourceCode)
                    .ShouldFixCodeWith(expectedCodeFix)
                    .ValidateAsync();
        }

        [TestMethod]
        public async Task InternalCtor()
        {
            var sourceCode = @"
abstract class Test
{
    internal [|]Test() { }
}";

            var codeFix = @"
abstract class Test
{
    protected Test() { }
}";

            await CreateProjectBuilder()
                    .WithSourceCode(sourceCode)
                    .ShouldFixCodeWith(codeFix)
                    .ValidateAsync();
        }
    }
}

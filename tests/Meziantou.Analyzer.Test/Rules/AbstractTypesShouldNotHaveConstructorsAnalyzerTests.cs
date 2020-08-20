using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class AbstractTypesShouldNotHaveConstructorsAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<AbstractTypesShouldNotHaveConstructorsAnalyzer>()
                .WithCodeFixProvider<AbstractTypesShouldNotHaveConstructorsFixer>();
        }

        [Fact]
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

        [Fact]
        public async Task PublicCtor()
        {
            var sourceCode = @"
abstract class Test
{
    public [||]Test() { }
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

        [Fact]
        public async Task InternalCtor()
        {
            var sourceCode = @"
abstract class Test
{
    internal [||]Test() { }
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

        [Fact]
        public async Task InternalCtor_BatchFix()
        {
            var sourceCode = @"
abstract class Test
{
    internal [||]Test() { }

    internal [||]Test(int a) { }
}";

            var codeFix = @"
abstract class Test
{
    protected Test() { }

    protected Test(int a) { }
}";

            await CreateProjectBuilder()
                    .WithSourceCode(sourceCode)
                    .ShouldBatchFixCodeWith(codeFix)
                    .ValidateAsync();
        }
    }
}

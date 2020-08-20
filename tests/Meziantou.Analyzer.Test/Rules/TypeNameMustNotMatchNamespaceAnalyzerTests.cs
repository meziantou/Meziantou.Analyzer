using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class TypeNameMustNotMatchNamespaceAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<TypeNameMustNotMatchNamespaceAnalyzer>();
        }

        [Fact]
        public async Task DifferentName()
        {
            const string SourceCode = @"
namespace TestNamespace
{
    class TestClass
    {
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task SameName()
        {
            const string SourceCode = @"
namespace Test
{
    class [||]Test
    {
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task SameNameInNestedType()
        {
            const string SourceCode = @"
namespace Test
{
    class TestClass
    {
        class Test
        {
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}

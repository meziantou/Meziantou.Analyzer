using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class TypeNameMustNotMatchNamespaceAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<TypeNameMustNotMatchNamespaceAnalyzer>();
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class DeclareTypesInNamespacesAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DeclareTypesInNamespacesAnalyzer>();
        }

        [TestMethod]
        public async Task InNamespace_IsValid()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
namespace Test
{
    class Sample
    {
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task NotInNamespace_IsInvalid()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
class [||]Sample
{
    class Nested { }
}")
                  .ValidateAsync();
        }
    }
}

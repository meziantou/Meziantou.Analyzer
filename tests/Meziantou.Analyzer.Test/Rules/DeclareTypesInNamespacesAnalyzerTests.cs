using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DeclareTypesInNamespacesAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DeclareTypesInNamespacesAnalyzer>();
        }

        [Fact]
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

        [Fact]
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

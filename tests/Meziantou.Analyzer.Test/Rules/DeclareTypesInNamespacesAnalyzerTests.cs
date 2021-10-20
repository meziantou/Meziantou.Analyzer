using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
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

        [Fact]
        public async Task TopLevelProgram()
        {
            await CreateProjectBuilder()
                  .WithOutputKind(OutputKind.ConsoleApplication)
                  .WithSourceCode(@"
System.Console.WriteLine();")
                  .ValidateAsync();
        }
    }
}

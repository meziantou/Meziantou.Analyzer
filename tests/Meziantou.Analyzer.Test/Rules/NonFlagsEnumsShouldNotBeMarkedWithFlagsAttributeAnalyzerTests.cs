using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class NonFlagsEnumsShouldNotBeMarkedWithFlagsAttributeAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<NonFlagsEnumsShouldNotBeMarkedWithFlagsAttributeAnalyzer>();
        }

        [TestMethod]
        public async Task NonPowerOfTwo()
        {
            const string SourceCode = @"
[System.Flags]
enum [|]Test : byte
{
    A = 1,
    B = 2,
    C = 5, // Non valid
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task PowerOfTwoOrCombination()
        {
            const string SourceCode = @"
[System.Flags]
enum Test : byte
{
    A = 1,
    B = 2,
    C = 3,
    D = 4,
    E = D | A,
    F = 8,
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task PowerOfTwoOrCombinationUsingHexa()
        {
            const string SourceCode = @"
[System.Flags]
enum Test
{
    A = 0x0,
    B = 0x1,
    C = 0x2,
    D = 0x4,
    E = 0x8,
    F = 0x10,
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}

using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class NonFlagsEnumsShouldNotBeMarkedWithFlagsAttributeAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<NonFlagsEnumsShouldNotBeMarkedWithFlagsAttributeAnalyzer>();
        }

        [Fact]
        public async Task NonPowerOfTwo()
        {
            const string SourceCode = @"
[System.Flags]
enum [||]Test : byte
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

        [Fact]
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

        [Fact]
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

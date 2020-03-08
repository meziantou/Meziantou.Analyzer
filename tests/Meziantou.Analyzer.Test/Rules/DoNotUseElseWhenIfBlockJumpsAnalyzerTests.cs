using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DoNotUseElseWhenIfBlockJumpsAnalyzerTests
    {
        private const string IfThatContinuesElse = @"
class TestClass
{
    int Test(int value)
    {
        while (true)
        {
            if (value < 0)
            {
                value++;
                continue;
            }
        [||]else
            {
                return 1;
            }
        }
    }
}";
        private const string IfThatReturnsElse = @"
class TestClass
{
    int Test(int value)
    {
        if (value < 0)
        {
            // Indicates it's a negative number
            return -1;
        }
    [||]else
        {
            // Indicates it's a positive number
            return 1;
        }
    }
}";
        private const string IfThatThrowsElse = @"
class TestClass
{
    int Test(int value)
    {
        if (value < 0)
        {
            throw new System.ArgumentNullException(nameof(value));
        }
    [||]else
        {
            // Indicates it's a positive number
            return 1;
        }
    }
}";
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseElseWhenIfBlockJumpsAnalyzer>();
        }

        [Theory]
        [InlineData(IfThatContinuesElse)]
        [InlineData(IfThatReturnsElse)]
        [InlineData(IfThatThrowsElse)]
        public async Task IfBlockJumpsAndElseBlockExists_DiagnosticIsReported(string sourceCode)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ShouldReportDiagnosticWithMessage("Do not use 'else' when 'if' block jumps")
                  .ValidateAsync();
        }

        [Fact]
        public async Task IfBlockJumpsAndNoElseBlockExists_NoDiagnosticIsReported()
        {
            var sourceCode = @"
class TestClass
{
    int Test(int value)
    {
        while (value-- > -10)
        {
            if (value < 0)
            {
                continue;
            }
            System.Console.WriteLine(""Value is still positive"");
        }
        if (value < -100)
            throw new System.InvalidOperationException(""Value is way too negative"");
        return 1;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task IfBlockDoesNotJumpAndElseBlockExists_NoDiagnosticIsReported()
        {
            var sourceCode = @"
class TestClass
{
    int Test(int value)
    {
        while (true)
        {
            if (value < 0)
            {
                value++;
                System.Console.WriteLine(""Value is negative"");
            }
            else
            {
                System.Console.WriteLine(""Value is positive"");
                break;
            }
        }

        return 1;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }
    }
}

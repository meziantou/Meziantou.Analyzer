using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DoNotUseElseWhenIfBlockJumpsAnalyzerTests
    {
        private const string IfBreakElse = @"
class TestClass
{
    void Test()
    {
        var value = -1;
        while (true)
        {
            if (value < 0)
            {
                Incr(ref value);
                break;
                void Incr(ref int val) => val++;
            }
        [||]else
            {
                value--;
            }
        }
    }
}";
        private const string IfContinueElse = @"
class TestClass
{
    void Test()
    {
        var value = -1;
        while (true)
        {
            if (value < 0)
            {
                Incr(ref value);
                continue;
                void Incr(ref int val) => val++;
            }
        [||]else
            {
                value--;
            }
        }
    }
}";
        private const string IfGotoElse = @"
class TestClass
{
    void Test()
    {
        var value = -1;
        while (true)
        {
            if (value < 0)
            {
                Incr(ref value);
                goto OUT;
                void Incr(ref int val) => val++;
            }
        [||]else
            {
                value--;
            }
        }
    OUT:
        value--;
    }
}";
        private const string IfReturnElse = @"
class TestClass
{
    void Test()
    {
        var value = -1;
        while (true)
        {
            if (value < 0)
            {
                return;
            }
        [||]else
            {
                value--;
            }
        }
    }
}";
        private const string IfThrowElse = @"
class TestClass
{
    void Test()
    {
        var value = -1;
        while (true)
        {
            if (value < 0)
                throw new System.ArgumentNullException(nameof(value));
        [||]else
                value--;
        }
    }
}";
        private const string IfYieldElse = @"
class TestClass
{
    System.Collections.Generic.IEnumerable<int> Test()
    {
        int value = -1;
        while (true)
        {
            if (value < 0)
            {
                value++;
                yield return value;
            }
        [||]else
                value--;
        }
    }
}";
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseElseWhenIfBlockJumpsAnalyzer>();
        }

        [Theory]
        [InlineData(IfBreakElse)]
        [InlineData(IfContinueElse)]
        [InlineData(IfGotoElse)]
        [InlineData(IfReturnElse)]
        [InlineData(IfThrowElse)]
        [InlineData(IfYieldElse)]
        public async Task IfBlockJumpsAndElseBlockExists_DiagnosticIsReported(string sourceCode)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ShouldReportDiagnosticWithMessage("Do not use 'else' when 'if' block jumps")
                  .ValidateAsync();
        }

        [Fact]
        public async Task IfBlockJumpsAndNoElseBlockExists_NoDiagnosticReported()
        {
            var sourceCode = @"
class TestClass
{
    void Test()
    {
        var value = -1;
        while (true)
        {
            if (value < 0)
            {
                continue;
            }
            value++;
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task IfBlockDoesNotJumpAndElseBlockExists_NoDiagnosticReported()
        {
            var sourceCode = @"
class TestClass
{
    void Test()
    {
        var value = -1;
        while (true)
        {
            if (value < 0)
            {
                value++;
            }
            else
            {
                break;
            }
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task InnerIfBlockJumpsAndOuterElseBlockExists_NoDiagnosticReported()
        {
            var sourceCode = @"
class TestClass
{
    void Test()
    {
        var value = -1;
        if (true)
        {
            if (true)
                return;
        }
        else
        {
            value++;
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }
    }
}

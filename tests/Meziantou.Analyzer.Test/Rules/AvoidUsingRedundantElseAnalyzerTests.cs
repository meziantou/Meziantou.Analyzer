using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class AvoidUsingRedundantElseAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<AvoidUsingRedundantElseAnalyzer>();
        }

        [Fact]
        public async Task IfBreakElse_DiagnosticIsReported()
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
                {
                    Incr(ref value);
                    break;
                }
                void Incr(ref int val) => val++;
            }
        [|else|]
            {
                value--;
            }
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task IfContinueElse_DiagnosticIsReported()
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
                Incr(ref value);
                continue;
                void Incr(ref int val) => val++;
            }
        [|else|]
            {
                value--;
            }
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task IfGotoElse_DiagnosticIsReported()
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
                Incr(ref value);
                goto OUT;
                void Incr(ref int val) => val++;
            }
        [|else|]
            {
                value--;
            }
        }
    OUT:
        value--;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task IfReturnElse_DiagnosticIsReported()
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
                return;
            }
        [|else|]
            {
                value--;
            }
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task IfThrowElse_DiagnosticIsReported()
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
                throw new System.ArgumentNullException(nameof(value));
        [|else|]
                value--;
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task IfYieldBreakElse_DiagnosticIsReported()
        {
            var sourceCode = @"
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
                yield break;
            }
        [|else|]
                value--;
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task IfYieldReturnElse_NoDiagnosticReported()
        {
            var sourceCode = @"
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
            else
                value--;
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task IfContinueNoElse_NoDiagnosticReported()
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
        public async Task IfNoJumpElse_NoDiagnosticReported()
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
        var value = new System.Random().Next(-10, 10);
        if (value < 0)
        {
            if (value < 5)
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
